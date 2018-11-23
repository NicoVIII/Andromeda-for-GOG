module Andromeda.Core.FSharp.Installed

open FSharp.Json
open GogApi.DotNet.FSharp.GalaxyApi
open GogApi.DotNet.FSharp.Listing
open System
open System.IO

open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Helpers

type UpdateData = {
    game: InstalledGame.T;
    newVersion: string;
}

type WindowsGameInfoFile = {
    gameId: string;
    name: string;
    version: int option;
}

let checkAllForUpdates appData =
    appData.installedGames
    |> List.filter (fun game -> game.updateable)
    |> List.fold (fun (lst, appData) game ->
        let (update, auth) = askForProductInfo appData.authentication { id = game.id }
        let appData = { appData with authentication = auth }
        match update with
        | None ->
            (lst, appData)
        | Some update ->
            let os =
                match os with
                | Linux -> Some "linux"
                | Windows -> Some "windows"
                | MacOS -> Some "mac"
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
                    if game.version = installer.version then
                        (lst, appData)
                    else
                        ({ newVersion = installer.version; game = game }::lst, appData)
            | None ->
                failwith "OS is invalid for some reason!"
    ) ([], appData)

let getGameId appData name =
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

let searchInstalled (appData :AppData) =
    // Code
    // TODO: Replace with appdata configuration
    let path =
        match os with
        | Linux ->
            Environment.GetEnvironmentVariable "HOME"
            |> sprintf "%s/GOG Games"
        | Windows ->
            "D:/Spiele"
        | MacOS ->
            "" // TODO:

    let appData = { appData with installedGames = [] }
    Directory.EnumerateDirectories(path)
    |> List.ofSeq
    |> List.fold (fun appData gameDir ->
        // Ignore folders starting with '!'
        match gameDir with
        | dir when dir |> Path.GetFileName |> String.startsWith "!" ->
            appData
        | gameDir ->
            let fnc =
                match os with
                | Linux ->
                    Some getInstalledOnLinux
                | Windows ->
                    Some getInstalledOnWindows
                | MacOS ->
                    None // TODO: implement
            match fnc with
            | Some fnc -> fnc appData gameDir
            | None -> appData
    ) appData
    |> fluent (saveAppData)
