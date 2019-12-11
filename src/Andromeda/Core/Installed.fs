module Andromeda.Core.FSharp.Installed

open FSharp.Json
open GogApi.DotNet.FSharp.GalaxyApi
open GogApi.DotNet.FSharp.Listing
open System.IO

open Andromeda.Core.FSharp.PersistenceTypes

type UpdateData = {
    game: InstalledGame;
    newVersion: string;
}

type WindowsGameInfoFile = {
    gameId: string;
    name: string;
    version: int option;
}

let checkAllForUpdates (appData: AppData) =
    appData.installedGames
    |> List.filter (fun game -> game.updateable)
    |> List.fold (fun (lst, (appData:AppData)) game ->
        let (update, auth) = askForProductInfo appData.authentication { id = game.id }
        let appData = { appData with authentication = auth }
        match update with
        | None ->
            (lst, appData)
        | Some update ->
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
                    |> List.first
                match installer with
                | None ->
                    (lst, appData)
                | Some installer ->
                    match (game.version, installer.version) with
                    | (a, Some b) when a <> b ->
                        ({ newVersion = b; game = game }::lst, appData)
                    | (_, _) ->
                        (lst, appData)
            | None ->
                failwith "OS is invalid for some reason!"
    ) ([], appData)

let getGameId (appData: AppData) name =
    askForFilteredProducts appData.authentication { search = name }
    |> exeFst (
        function
        | None -> None
        | Some x ->
            List.filter (fun p -> p.title = name) x.products
            |>  function
                | l when l.Length = 1 ->
                    x.products.Head.id
                    |> Some
                | l when l.Length >= 0 ->
                    None
                | _ -> failwith "Something went totally wrong! Gog reported a negative amount of products..."
    )
    |> exeSnd (
        fun auth -> { appData with authentication = auth }
    )

let getInstalledOnLinux (appData: AppData) gameDir =
    // Internal functions
    let readGameinfo path :string[] option =
        let gameinfoPath = sprintf "%s/gameinfo" path
        match File.Exists gameinfoPath with
        | true ->
            gameinfoPath
            |> File.ReadAllLines
            |> Some
        | false ->
            None

    // Code
    match readGameinfo gameDir with
    | None ->
        appData
    | Some lines when lines.Length > 3 ->
        let game =
            InstalledGame.create ((int)lines.[4]) lines.[0] gameDir lines.[1]
            |> InstalledGame.setUpdateable true
        let installed = game::appData.installedGames
        { appData with installedGames = installed }
    | Some lines ->
        let idData =
            Path.GetFileName gameDir
            |> getGameId appData
        match idData with
        | (None, appData) ->
            appData
        | (Some id, appData) ->
            let game =
                InstalledGame.create id lines.[0] gameDir lines.[1]
            let installed = game::appData.installedGames
            { appData with installedGames = installed }

let getInstalledOnWindows (appData: AppData) gameDir =
    let extractGameInfo file =
        let gameInfo =
            File.ReadAllLines file
            |> Seq.fold (+) ""
            |> Json.deserialize<WindowsGameInfoFile>
        // TODO: determine version and updateability
        InstalledGame.create ((int)gameInfo.gameId) gameInfo.name gameDir "1"
        |> InstalledGame.setIcon (Some (gameDir + "/goggame-" + gameInfo.gameId + ".ico"))

    // Find info file of game
    let files = Directory.GetFiles (gameDir, "goggame-*.info")
    match files.Length with
    | 1 ->
        let game = extractGameInfo files.[0]
        let installed = game::appData.installedGames
        { appData with installedGames = installed }
    | _ ->
        appData

let searchInstalled (saveAppData: SaveAppData) (appData :AppData) =
    let appData = { appData with installedGames = [] }
    Directory.EnumerateDirectories(appData.settings.gamePath)
    |> List.ofSeq
    |> List.fold (fun appData gameDir ->
        // Ignore folders starting with '!'
        match gameDir with
        | dir when dir |> Path.GetFileName |> String.startsWith "!" ->
            appData
        | gameDir ->
            let fnc =
                match SystemInfo.os with
                | SystemInfo.OS.Linux ->
                    Some getInstalledOnLinux
                | SystemInfo.OS.Windows ->
                    Some getInstalledOnWindows
                | SystemInfo.OS.MacOS ->
                    failwith "Not implemented yet" // TODO: implement
            match fnc with
            | Some fnc -> fnc appData gameDir
            | None -> appData
    ) appData
    |> fluent (saveAppData >> ignore)
