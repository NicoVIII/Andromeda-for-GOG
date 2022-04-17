namespace Andromeda.Core

open FSharp.Json
open GogApi
open GogApi.DomainTypes
open Mono.Unix.Native
open System.Diagnostics
open System.IO

open Andromeda.Core.DomainTypes
open Andromeda.Core.Helpers
open SimpleOptics

/// A module for everything which works on installed games
module Installed =
    type UpdateData = { game: Game; newVersion: string }

    type WindowsGameInfoFile =
        { gameId: string
          name: string
          version: int option }

    /// Looks up, if there is an update available for given game
    let checkGameForUpdate authentication game =
        async {
            let! result =
                Helpers.withAutoRefresh (GalaxyApi.getProduct game.id) authentication

            return
                match result with
                | (Error _, authentication) -> (None, authentication)
                | (Ok update, authentication) ->
                    let os =
                        match SystemInfo.os with
                        | SystemInfo.OS.Linux -> "linux"
                        | SystemInfo.OS.Windows -> "windows"
                        | SystemInfo.OS.MacOS -> "mac"

                    // Get first installer for os
                    let installer =
                        update.downloads.installers
                        |> List.filter (fun installer -> installer.os = os)
                        |> List.item 0

                    // Return game, if update is available
                    match (game.status, installer.version) with
                    | (Installed (version, _), Some availableVersion) when
                        version <> availableVersion
                        ->
                        (Some
                            { newVersion = availableVersion
                              game = game },
                         authentication)
                    | _ -> (None, authentication)
        }

    let private prepareGameProcess processOutput (proc: Process) =
        proc.StartInfo.RedirectStandardOutput <- true
        proc.StartInfo.RedirectStandardError <- true
        proc.OutputDataReceived.AddHandler(new DataReceivedEventHandler(processOutput))
        proc.ErrorDataReceived.AddHandler(new DataReceivedEventHandler(processOutput))
        proc

    let private startWindowsGameProcess (prepareGameProcess: Process -> Process) path =
        let file =
            Directory.GetFiles(path)
            |> List.ofArray
            |> List.filter (fun path ->
                let fileName = Path.GetFileName path

                fileName.StartsWith "Launch "
                && fileName.EndsWith ".lnk")
            |> List.item 0

        let proc =
            new Process()
            |> fun proc ->
                proc.StartInfo.FileName <- Path.getShortcutTarget file
                proc
            |> prepareGameProcess

        try
            proc.Start() |> ignore
            proc.BeginOutputReadLine()
            proc |> Some
        with
        | _ ->
            try
                // Try again with admin rights
                proc.StartInfo.UseShellExecute <- true
                proc.StartInfo.Verb <- "runas"
                proc.Start() |> ignore
                proc.BeginOutputReadLine()
                Some proc
            with
            | _ -> None

    let startGameProcess processStandardOutput path =
        let prepareGameProcess = prepareGameProcess processStandardOutput

        match SystemInfo.os with
        | SystemInfo.OS.Linux
        | SystemInfo.OS.MacOS ->
            let filepath = Path.Combine(path, "start.sh")

            Syscall.chmod (filepath, FilePermissions.ALLPERMS)
            |> ignore

            let proc =
                new Process()
                |> fun proc ->
                    proc.StartInfo.FileName <- filepath
                    proc
                |> prepareGameProcess

            proc.Start() |> ignore
            proc.BeginOutputReadLine()
            proc.BeginErrorReadLine()
            proc |> Some
        | SystemInfo.OS.Windows -> startWindowsGameProcess prepareGameProcess path

    let getGameId (authentication: Authentication) name =
        Account.getFilteredGames
            { feature = None
              page = None
              search = Some name
              sort = None
              language = None
              system = None }
            authentication
        |> Async.RunSynchronously
        |> function
            | Error _ -> None
            | Ok x ->
                x.products
                |> List.filter (fun p -> p.title = name)
                |> function
                    | l when l.Length = 1 -> x.products.Head.id |> Some
                    | l when l.Length >= 0 -> None
                    | _ ->
                        failwith
                            "Something went totally wrong! Gog reported a negative amount of products..."

    let getInstalledOnLinux gameDir (authentication: Authentication) version =
        // Internal functions
        let readGameinfo path : string [] option =
            let gameinfoPath = sprintf "%s/gameinfo" path

            match File.Exists gameinfoPath with
            | true -> gameinfoPath |> File.ReadAllLines |> Some
            | false -> None

        // Code
        match readGameinfo gameDir with
        | None -> None
        | Some lines when lines.Length > 3 ->
            let version =
                match version with
                | Some version -> version
                | None -> lines.[1]

            let game =
                Game.create (lines.[4] |> uint32 |> ProductId) lines.[0]
                |> Optic.set GameOptic.status (Installed(version, gameDir))
                |> Optic.set GameOptic.updateable true
                |> Some

            game
        | Some lines ->
            let version =
                match version with
                | Some version -> version
                | None -> lines.[1]

            Path.GetFileName gameDir
            |> getGameId authentication
            |> function
                | None -> None
                | Some id ->
                    let game =
                        Game.create id lines.[0]
                        |> Optic.set GameOptic.status (Installed(version, gameDir))
                        |> Some

                    game

    let getInstalledOnWindows gameDir (_: Authentication) version =
        let extractGameInfo file =
            let gameInfo =
                File.ReadAllLines file
                |> Seq.fold (+) ""
                |> Json.deserialize<WindowsGameInfoFile>
            // TODO: determine version and updateability
            let versionString =
                match version with
                | Some version -> version
                | None -> "1" // TODO:

            Game.create (gameInfo.gameId |> uint32 |> ProductId) gameInfo.name
            |> Optic.set GameOptic.status (Installed(versionString, gameDir))
            |> Optic.set GameOptic.updateable version.IsSome

        // Find info file of game
        let files = Directory.GetFiles(gameDir, "goggame-*.info")

        match files.Length with
        | 1 ->
            let game = extractGameInfo files.[0] |> Some
            game
        | _ -> None

    let readVersionFromFile gameDir =
        let versionFilePath = Path.Combine(gameDir, Constants.versionFile)

        if versionFilePath |> File.Exists then
            versionFilePath
            |> File.ReadAllText
            |> (fun s -> s.Trim())
            |> Some
        else
            None

    let searchInstalled (settings: Settings) (authentication: Authentication) =
        let emptyResult = (Map.empty, [])

        match Directory.Exists(settings.gamePath) with
        | false -> emptyResult
        | true ->
            Directory.EnumerateDirectories(settings.gamePath)
            |> Seq.fold
                (fun state gameDir ->
                    // Ignore folders starting with '!'
                    match gameDir with
                    | dir when (dir |> Path.GetFileName).StartsWith "!" -> state
                    | gameDir ->
                        // Read version file if existing
                        let version = readVersionFromFile gameDir

                        // Choose correct algorithm for each OS
                        let fnc =
                            match SystemInfo.os with
                            | SystemInfo.OS.Linux -> getInstalledOnLinux
                            | SystemInfo.OS.Windows -> getInstalledOnWindows
                            | SystemInfo.OS.MacOS -> (fun _ _ _ -> None) // TODO: implement

                        let installedGame = fnc gameDir authentication version

                        match installedGame with
                        | Some installedGame ->
                            let (installedGames, imgJobs) = state

                            let (installedGame, imgJobs) =
                                match Diverse.getProductImg installedGame.id with
                                | Diverse.AlreadyDownloaded imgPath ->
                                    (Optic.set
                                        GameOptic.image
                                        (Some imgPath)
                                        installedGame),
                                    imgJobs
                                | Diverse.HasToBeDownloaded job ->
                                    installedGame, (job :: imgJobs)

                            // TODO: Replace image jobs with "LookupGameImage" msg
                            (Map.add installedGame.id installedGame installedGames,
                             imgJobs)
                        | None -> state)
                emptyResult
