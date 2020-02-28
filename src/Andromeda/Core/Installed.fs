module Andromeda.Core.FSharp.Installed

open FSharp.Json
open GogApi.DotNet.FSharp.GalaxyApi
open GogApi.DotNet.FSharp.Listing
open System.IO

type UpdateData =
    { game: InstalledGame
      newVersion: string }

type WindowsGameInfoFile =
    { gameId: string
      name: string
      version: int option }

let checkAllForUpdates (installedGames: InstalledGame list) (authentication: Authentication) =
    installedGames
    |> List.filter (fun game -> game.updateable)
    |> List.fold (fun (lst, authentication: Authentication) game ->
        askForProductInfo { id = game.id } authentication
        |> Async.RunSynchronously
        |> function
        | Error _ -> (lst, authentication)
        | Ok update ->
            let os =
                match SystemInfo.os with
                | SystemInfo.OS.Linux -> Some "linux"
                | SystemInfo.OS.Windows -> Some "windows"
                | SystemInfo.OS.MacOS -> Some "mac"
            match os with
            | Some os ->
                let installer =
                    update.downloads.installers
                    |> List.filter (fun installer -> installer.os = os)
                    |> List.item 0
                match (game.version, installer.version) with
                | (a, Some b) when a <> b ->
                    ({ newVersion = b
                       game = game }
                     :: lst, authentication)
                | (_, _) -> (lst, authentication)
            | None -> failwith "OS is invalid for some reason!") ([], authentication)

let private getGameId (authentication: Authentication) name =
    askForFilteredProducts { search = name } authentication
    |> Async.RunSynchronously
    |> function
    | Error _ -> None
    | Ok x ->
        List.filter (fun p -> p.title = name) x.products
        |> function
        | l when l.Length = 1 -> x.products.Head.id |> Some
        | l when l.Length >= 0 -> None
        | _ -> failwith "Something went totally wrong! Gog reported a negative amount of products..."

let getInstalledOnLinux gameDir (authentication: Authentication) =
    // Internal functions
    let readGameinfo path: string [] option =
        let gameinfoPath = sprintf "%s/gameinfo" path
        match File.Exists gameinfoPath with
        | true ->
            gameinfoPath
            |> File.ReadAllLines
            |> Some
        | false -> None

    // Code
    match readGameinfo gameDir with
    | None -> None
    | Some lines when lines.Length > 3 ->
        let game =
            InstalledGame.create ((int) lines.[4]) lines.[0] gameDir lines.[1]
            |> InstalledGame.setUpdateable true
            |> Some
        game
    | Some lines ->
        Path.GetFileName gameDir
        |> getGameId authentication
        |> function
        | None -> None
        | Some id ->
            let game = InstalledGame.create id lines.[0] gameDir lines.[1] |> Some
            game

let getInstalledOnWindows gameDir (_: Authentication) =
    let extractGameInfo file =
        let gameInfo =
            File.ReadAllLines file
            |> Seq.fold (+) ""
            |> Json.deserialize<WindowsGameInfoFile>
        // TODO: determine version and updateability
        InstalledGame.create ((int) gameInfo.gameId) gameInfo.name gameDir "1"
        |> InstalledGame.setIcon (Some(gameDir + "/goggame-" + gameInfo.gameId + ".ico"))

    // Find info file of game
    let files = Directory.GetFiles(gameDir, "goggame-*.info")
    match files.Length with
    | 1 ->
        let game = extractGameInfo files.[0] |> Some
        game
    | _ -> None

let searchInstalled (settings: Settings) (authentication: Authentication) =
    Directory.EnumerateDirectories(settings.gamePath)
    |> List.ofSeq
    |> List.fold (fun installedGames gameDir ->
        // Ignore folders starting with '!'
        match gameDir with
        | dir when dir
                   |> Path.GetFileName
                   |> String.startsWith "!" -> installedGames
        | gameDir ->
            let fnc =
                match SystemInfo.os with
                | SystemInfo.OS.Linux -> Some getInstalledOnLinux
                | SystemInfo.OS.Windows -> Some getInstalledOnWindows
                | SystemInfo.OS.MacOS -> None // TODO: implement
            match fnc with
            | Some fnc ->
                let installedGame = fnc gameDir authentication
                match installedGame with
                | Some installedGame -> installedGame :: installedGames
                | None -> installedGames
            | None -> installedGames) []
