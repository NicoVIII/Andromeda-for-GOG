module Andromeda.Core.FSharp.AppData

open Couchbase.Lite
open GogApi.DotNet.FSharp.Authentication
open GogApi.DotNet.FSharp.Base
open System.IO

let createBasicAppData (): AppData = { authentication = NoAuth; installedGames = []; gamePath = SystemInfo.gamePath }
let withNewToken appData code = { appData with authentication = newToken(code) }

let dbConfig = DatabaseConfiguration()
Directory.CreateDirectory(SystemInfo.savePath) |> ignore
dbConfig.Directory <- SystemInfo.savePath

let saveAppData appData =
    use db = new Database("andromeda", dbConfig)
    use doc =
        match db.GetDocument "appdata" with
        | null -> new MutableDocument("appdata")
        | x -> x.ToMutable ()

    // Authentication
    let auth = appData.authentication
    match auth with
    | NoAuth -> ()
    | Auth auth ->
        let authDict = MutableDictionaryObject ()
        authDict.SetString ("accesscode", auth.accessToken) |> ignore
        authDict.SetString ("refreshcode", auth.refreshToken) |> ignore
        doc.SetDictionary ("authentication", authDict) |> ignore

    // Installed games
    let gamesArray = MutableArrayObject ()
    List.fold (fun _ (info: InstalledGame) ->
        let dict = MutableDictionaryObject ()
        dict.SetInt ("id", info.id) |> ignore
        dict.SetString ("name", info.name) |> ignore
        dict.SetString ("path", info.path) |> ignore
        dict.SetString ("version", info.version) |> ignore
        dict.SetBoolean ("updateable", info.updateable) |> ignore
        let icon =
            match info.icon with
            | Some icon -> icon
            | None -> ""
        dict.SetString ("icon", icon) |> ignore
        gamesArray.AddDictionary dict |> ignore
    ) () appData.installedGames
    doc.SetArray ("installed", gamesArray) |> ignore

    db.Save doc
    db.Close ()

let loadAppData () =
    use db = new Database ("andromeda", dbConfig)
    use doc = db.GetDocument("appdata")

    let appData =
        match doc with
        | null ->
            createBasicAppData ()
        | doc ->
            // Authentication
            let auth =
                let dict = doc.GetDictionary "authentication"
                match dict with
                | null -> NoAuth
                | dict ->
                    Auth {
                        accessToken = dict.GetString "accesscode";
                        refreshToken = dict.GetString "refreshcode";
                        refreshed = false;
                    }

            // Installed games
            let installed =
                doc.GetArray "installed"
                |> convertFromArrayObject (fun index array ->
                    array.GetDictionary index
                )
                |> List.map (fun dict ->
                    InstalledGame.create (dict.GetInt "id") (dict.GetString "name") (dict.GetString "path") (dict.GetString "version")
                    |> InstalledGame.setUpdateable (dict.GetBoolean "updateable")
                    |> InstalledGame.setIcon (
                        match dict.GetString "icon" with
                        | "" -> None
                        | icon -> Some icon
                    )
                )
            // TODO: save and load game path as well
            { authentication = auth; installedGames = installed; gamePath = SystemInfo.gamePath }
    db.Close ()
    appData
