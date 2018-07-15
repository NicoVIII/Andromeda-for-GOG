module Andromeda.Core.FSharp.Installed

open Couchbase.Lite
open HttpFs.Client
open System
open System.IO

open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Helpers
open Andromeda.Core.FSharp.Responses

let getGameInfo (GameId id) auth =
    makeRequest<GameInfoResponse> Get auth [ ] (sprintf "https://embed.gog.com/account/gameDetails/%i.json" id)

let saveInstalledGames installed =
    use db = new Database("andromeda")
    use doc =
        match db.GetDocument "installed" with
        | null -> new MutableDocument("installed")
        | x -> x.ToMutable ()
    let mutable array = new MutableArrayObject ()
    array <- List.fold (fun array info ->
        let { id = GameId id; name = name; path = GamePath path; version = version } = info
        let dict = new MutableDictionaryObject ()
        dict.SetInt ("id", id) |> ignore
        dict.SetString ("name", name) |> ignore
        dict.SetString ("path", path) |> ignore
        dict.SetString ("version", version) |> ignore
        array.AddDictionary dict |> ignore
        array
    ) array installed
    doc.SetArray ("installed", array) |> ignore
    db.Save doc
    db.Close ()

let loadInstalledGames () =
    use db = new Database ("andromeda")
    use doc = db.GetDocument("installed")
    db.Close ()
    match doc with
    | null ->
        []
    | doc ->
        doc.GetArray "installed"
        |> convertFromArrayObject (fun index array ->
            array.GetDictionary index
        )
        |> List.map (fun dict ->
            {
                id = dict.GetInt "id" |> GameId;
                name = dict.GetString "name";
                path = dict.GetString "path" |> GamePath;
                version = dict.GetString "version"
            }
        )

let searchInstalled () =
    let path =
        match getOS () with
        | Linux ->
            Environment.GetEnvironmentVariable "HOME"
            |> sprintf "%s/GOG Games"  // TODO:
        | Windows ->
            "" // TODO:
        | MacOS ->
            "" // TODO:
        | Unknown ->
            failwith "Something went wrong while determining the system os!"
    Directory.EnumerateDirectories(path)
    |> List.ofSeq
    |> List.map (fun gameDir ->
        let lines =
            sprintf "%s/gameinfo" gameDir
            |> File.ReadAllLines
        { id = GameId ((int)lines.[4]); name = lines.[0]; path = GamePath gameDir; version = lines.[1] }
    )
    |> fluent saveInstalledGames
