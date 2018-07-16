module Andromeda.Core.FSharp.Installed

open HttpFs.Client
open System
open System.IO

open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Helpers
open Andromeda.Core.FSharp.Responses

let getGameInfo (GameId id) auth =
    makeRequest<GameInfoResponse> Get auth [ ] (sprintf "https://embed.gog.com/account/gameDetails/%i.json" id)

let checkForUpdates appData (GameId id) =
    sprintf "https://api.gog.com/products/%i" id
    |> makeRequest<GameInfoResponse> Get appData [ createQuery "expand" "downloads" ]

let checkAllForUpdates appData =
    appData.installedGames
    |> List.fold (fun (lst, appData) game ->
        let (update, appData) = checkForUpdates appData game.id
        match update with
        | None ->
            (lst, appData)
        | Some update ->
            let os =
                match getOS () with
                | Linux -> "linux"
                | Windows -> "windows"
                | MacOS -> "mac"
            let version =
                update.downloads.installers
                |> List.filter (fun installer -> installer.os = os)
                |> fluent (List.iter (fun installer -> printfn "%s" installer.version))
                |> List.first
            if game.version = version.Value.version then
                (lst, appData)
            else
                (update::lst, appData)
    ) ([], appData)

let searchInstalled appData =
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
    let installed =
        Directory.EnumerateDirectories(path)
        |> List.ofSeq
        |> List.map (fun gameDir ->
            let lines =
                sprintf "%s/gameinfo" gameDir
                |> File.ReadAllLines
            { id = GameId ((int)lines.[4]); name = lines.[0]; path = GamePath gameDir; version = lines.[1] }
        )

    { appData with installedGames = installed}
    |> fluent (saveAppData)
