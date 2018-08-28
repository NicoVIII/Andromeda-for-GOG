module Andromeda.Core.FSharp.Installed

open GogApi.DotNet.FSharp.GalaxyApi
open GogApi.DotNet.FSharp.Listing
open System
open System.IO

open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Helpers

type UpdateData = {
    game: InstalledGame;
    newVersion: Version;
}

let checkAllForUpdates appData =
    appData.installedGames
    |> List.filter (fun game -> game.updateable)
    |> List.fold (fun (lst, appData) game ->
        let (GameId id) = game.id
        let (update, auth) = askForProductInfo appData.authentication { id = id }
        let appData = { appData with authentication = auth }
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
    askForFilteredProducts appData.authentication { search = name }
    |> exeFst (
        function
        | None -> None
        | Some x ->
            List.filter (fun p -> p.title = name) x.products
            |>  function
                | l when l.Length = 1 ->
                    GameId x.products.Head.id
                    |> Some
                | l when l.Length >= 0 ->
                    None
                | _ -> failwith "Something went totally wrong! Gog reported a negative amount of products..."
    )
    |> exeSnd (
        fun auth -> { appData with authentication = auth }
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
