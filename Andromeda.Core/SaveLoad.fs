module Andromeda.Core.FSharp.SaveLoad

open Couchbase.Lite
open Andromeda.Core.FSharp.Basics

let saveAuth auth =
    match auth with
    | Empty ->
        ()
    | Auth { refreshToken = refreshToken; accessToken = accessToken } ->
        let db = new Database ("andromeda")
        let doc =
            match db.GetDocument "auth" with
            | null -> new MutableDocument("auth")
            | x -> x.ToMutable ()
        doc.SetString ("refresh-token", refreshToken) |> ignore
        doc.SetString ("access-token", accessToken) |> ignore
        db.Save doc

let loadAuth () =
    let db = new Database ("andromeda")
    let doc = db.GetDocument("auth")
    match doc with
    | null -> Empty
    | doc ->
        Auth { refreshToken = doc.GetString("refresh-token"); accessToken = doc.GetString("access-token"); refreshed = false }
