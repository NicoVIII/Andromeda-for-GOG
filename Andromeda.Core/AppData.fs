module Andromeda.Core.FSharp.AppData

open Couchbase.Lite

open Andromeda.Core.FSharp.Helpers

type AuthenticationData = {
    accessToken: string;
    refreshToken: string;
    refreshed: bool;
}

type Authentication = NoAuth | Auth of AuthenticationData

type GameId = GameId of int
type Version = Version of string

type GamePath = GamePath of string

type InstalledGame = {
    id: GameId;
    name: string;
    path: GamePath;
    version: Version;
    updateable: bool;
}

type AppData = {
    authentication: Authentication;
    installedGames: InstalledGame list;
}

let createBasicAppData () = { authentication = NoAuth; installedGames = [] }

let saveAppData appData =
    use db = new Database("andromeda")
    use doc =
        match db.GetDocument "appdata" with
        | null -> new MutableDocument("appdata")
        | x -> x.ToMutable ()

    // Authentication
    let auth = appData.authentication
    match auth with
    | NoAuth -> ()
    | Auth auth ->
        let authDict = new MutableDictionaryObject ()
        authDict.SetString ("accesscode", auth.accessToken) |> ignore
        authDict.SetString ("refreshcode", auth.refreshToken) |> ignore
        doc.SetDictionary ("authentication", authDict) |> ignore

    // Installed games
    let gamesArray = new MutableArrayObject ()
    List.fold (fun _ info ->
        let { id = GameId id; name = name; path = GamePath path; version = Version version; updateable = updateable } = info
        let dict = new MutableDictionaryObject ()
        dict.SetInt ("id", id) |> ignore
        dict.SetString ("name", name) |> ignore
        dict.SetString ("path", path) |> ignore
        dict.SetString ("version", version) |> ignore
        dict.SetBoolean ("updateable", updateable) |> ignore
        gamesArray.AddDictionary dict |> ignore
    ) () appData.installedGames
    doc.SetArray ("installed", gamesArray) |> ignore

    db.Save doc
    db.Close ()

let loadAppData () =
    use db = new Database ("andromeda")
    use doc = db.GetDocument("appdata")

    let appData =
        match doc with
        | null ->
            createBasicAppData ()
        | doc ->
            // Authentication
            let auth =
                let dict = doc.GetDictionary "authentication"
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
                    {
                        id = dict.GetInt "id" |> GameId;
                        name = dict.GetString "name";
                        path = dict.GetString "path" |> GamePath;
                        version = dict.GetString "version" |> Version;
                        updateable = dict.GetBoolean "updateable";
                    }
                )
            { authentication = auth; installedGames = installed }
    db.Close ()
    appData
