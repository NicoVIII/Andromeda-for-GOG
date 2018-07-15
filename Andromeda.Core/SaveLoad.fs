module Andromeda.Core.FSharp.SaveLoad

open Couchbase.Lite

open Andromeda.Core.FSharp.Basics

let saveAuth auth =
    use db = new Database ("andromeda")
    use doc =
        match db.GetDocument "auth" with
        | null -> new MutableDocument("auth")
        | x -> x.ToMutable ()
    match auth with
    | NoAuth ->
        db.Delete doc
    | Auth { refreshToken = refreshToken; accessToken = accessToken } ->
        doc.SetString ("refresh-token", refreshToken) |> ignore
        doc.SetString ("access-token", accessToken) |> ignore
        db.Save doc
    db.Close ()

let loadAuth () =
    use db = new Database ("andromeda")
    use doc = db.GetDocument("auth")
    db.Close ()
    match doc with
    | null ->
        NoAuth
    | doc ->
        Auth { refreshToken = doc.GetString("refresh-token"); accessToken = doc.GetString("access-token"); refreshed = false }

