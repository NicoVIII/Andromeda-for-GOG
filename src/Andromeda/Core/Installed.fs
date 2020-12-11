namespace Andromeda.Core

open FSharp.Json
open GogApi.DotNet.FSharp
open GogApi.DotNet.FSharp.DomainTypes
open Mono.Unix.Native
open System
open System.Diagnostics
open System.IO
open System.Net

open Andromeda.Core.DomainTypes
open Andromeda.Core.Helpers
open Andromeda.Core.Lenses

/// A module for everything which works on installed games
module Installed =
    type UpdateData =
        { game: InstalledGame
          newVersion: string }

    type WindowsGameInfoFile =
        { gameId: string
          name: string
          version: int option }

    /// Looks up, if there is an update available for given game
    let checkGameForUpdate authentication game =
        Helpers.withAutoRefresh (GalaxyApi.getProduct game.id) authentication
        |> Async.RunSynchronously
        |> function
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
            match (game.version, installer.version) with
            | (a, Some b) when a <> b ->
                (Some { newVersion = b; game = game }, authentication)
            | _ -> (None, authentication)

    let checkAllForUpdates
        (installedGames: InstalledGame list)
        (authentication: Authentication)
        =
        installedGames
        |> List.filter (fun game -> game.updateable)
        |> List.fold (fun (lst, authentication: Authentication) game ->
            let (game, authentication) = checkGameForUpdate authentication game
            // Add game to list, if game is updateable
            match game with
            | Some game -> (game :: lst, authentication)
            | None -> (lst, authentication)
            ) ([], authentication)

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
        with _ ->
            try
                // Try again with admin rights
                proc.StartInfo.UseShellExecute <- true
                proc.StartInfo.Verb <- "runas"
                proc.Start() |> ignore
                proc.BeginOutputReadLine()
                Some proc
            with _ -> None

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
              system = None } authentication
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
        let readGameinfo path: string [] option =
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
                InstalledGame.create (lines.[4] |> uint32 |> ProductId) lines.[0] gameDir
                    version
                |> setl InstalledGameLenses.updateable true
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
                    InstalledGame.create id lines.[0] gameDir version
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

            InstalledGame.create (gameInfo.gameId |> uint32 |> ProductId) gameInfo.name
                gameDir versionString
            |> setl InstalledGameLenses.updateable version.IsSome
            |> setl InstalledGameLenses.icon
                   (Some(gameDir + "/goggame-" + gameInfo.gameId + ".ico"))

        // Find info file of game
        let files =
            Directory.GetFiles(gameDir, "goggame-*.info")

        match files.Length with
        | 1 ->
            let game = extractGameInfo files.[0] |> Some
            game
        | _ -> None

    let searchInstalled (settings: Settings) (authentication: Authentication) =
        match Directory.Exists(settings.gamePath) with
        | false -> []
        | true ->
            Directory.EnumerateDirectories(settings.gamePath)
            |> List.ofSeq
            |> List.fold (fun installedGames gameDir ->
                // Ignore folders starting with '!'
                match gameDir with
                | dir when (dir |> Path.GetFileName).StartsWith "!" -> installedGames
                | gameDir ->
                    // Read version file if existing
                    let version =
                        let versionFilePath =
                            Path.Combine(gameDir, Constants.versionFile)

                        if versionFilePath |> File.Exists then
                            versionFilePath
                            |> File.ReadAllText
                            |> (fun s -> s.Trim())
                            |> Some
                        else
                            None

                    let fnc =
                        match SystemInfo.os with
                        | SystemInfo.OS.Linux -> Some getInstalledOnLinux
                        | SystemInfo.OS.Windows -> Some getInstalledOnWindows
                        | SystemInfo.OS.MacOS -> None // TODO: implement

                    match fnc with
                    | Some fnc ->
                        let installedGame = fnc gameDir authentication version
                        match installedGame with
                        | Some installedGame -> installedGame :: installedGames
                        | None -> installedGames
                    | None -> installedGames) []
