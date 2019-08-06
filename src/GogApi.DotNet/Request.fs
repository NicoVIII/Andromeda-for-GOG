module GogApi.DotNet.FSharp.Request

open System.IO

open GogApi.DotNet.FSharp.Base
open GogApi.DotNet.FSharp.Helpers

let rec makeRequest<'T> method auth queries url :'T option * Authentication =
    let result = makeBasicJsonRequest method auth queries url
    match result with
    | Success parsed ->
        (Some parsed, auth)
    | Failure (raw, message) ->
        match auth with
        | Auth x ->
            match x with
            | { refreshed = true } ->
                printfn "Returned Json is not valid! Refreshing the authentication did not work."
                printfn "%A" raw
                printfn "%A - %A" message url
                (None, Auth { x with refreshed = false})
            | { refreshed = false } ->
                // Refresh authentication
                let auth' = Authentication.refresh x
                let appData = auth'
                makeRequest<'T> method appData queries url
        | NoAuth ->
            printfn "No authentication was given. Maybe valid authentication is necessary?"
            (None, NoAuth)
