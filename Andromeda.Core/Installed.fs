module Andromeda.Core.FSharp.Installed

open HttpFs.Client
open System
open System.IO

open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Helpers
open Andromeda.Core.FSharp.Responses

type UpdateData = {
    game: InstalledGame;
    newVersion: Version;
}

let getGameInfo (GameId id) auth =
    makeRequest<GameInfoResponse> Get auth [ ] (sprintf "https://embed.gog.com/account/gameDetails/%i.json" id)

let checkForUpdates appData (GameId id) =
    sprintf "https://api.gog.com/products/%i" id
    |> makeRequest<GameInfoResponse> Get appData [ createQuery "expand" "downloads" ]

let checkAllForUpdates appData =
    appData.installedGames
    |> List.filter (fun game -> game.updateable)
    |> List.fold (fun (lst, appData) game ->
        let (update, appData) = checkForUpdates appData game.id
        match update with
        | None ->
            (lst, appData)
        | Some update ->
            let os =
                match getOS () with
                | Linux -> Some "linux"
                | Windows -> Some "windows"
                | MacOS -> Some "mac"
                | Unknown -> None
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
                    let (Version version) = game.version
                    if version = installer.version then
                        (lst, appData)
                    else
                        ({ newVersion = Version installer.version; game = game }::lst, appData)
            | None ->
                failwith "OS is invalid for some reason!"
    ) ([], appData)

let getGameId appData name =
    let queries = [
        createQuery "search" name;
        createQuery "mediaType" "1";
    ]
    makeRequest<FilteredProductsResponse> Get appData queries "https://embed.gog.com/account/getFilteredProducts"
    |> exeFst (
        function
        | None -> None
        | Some x ->
            List.filter (fun p -> p.title = name) x.products
            |>  function
                | l when l.Length = 1 ->
                    GameId x.products.Head.id
                    |> Some
                | l when l.Length > 0 ->
                    None
                | _ -> failwith "Something went totally wrong! Gog reported a negative amount of products..."
    )

let searchInstalled (appData :AppData) =
    let path =
        match getOS () with
        | Linux ->
            Environment.GetEnvironmentVariable "HOME"
            |> sprintf "%s/GOG Games"
        | Windows ->
            "" // TODO:
        | MacOS ->
            "" // TODO:
        | Unknown ->
            failwith "Something went wrong while determining the system os!"

    let appData = { appData with installedGames = [] }
    Directory.EnumerateDirectories(path)
    |> List.ofSeq
    |> List.fold (fun appData gameDir ->
        let lines =
            sprintf "%s/gameinfo" gameDir
            |> File.ReadAllLines
        match lines with
        | lines when lines.Length > 3 ->
            let game = { id = GameId ((int)lines.[4]); name = lines.[0]; path = GamePath gameDir; version = Version lines.[1]; updateable = true }
            let installed = game::appData.installedGames
            { appData with installedGames = installed }
        | lines ->
            let idData =
                Path.GetFileName gameDir
                |> getGameId appData
            match idData with
            | (None, appData) ->
                appData
            | (Some id, appData) ->
                let game = { id = id; name = lines.[0]; path = GamePath gameDir; version = Version lines.[1]; updateable = false }
                let installed = game::appData.installedGames
                { appData with installedGames = installed }
    ) appData
    |> fluent (saveAppData)
