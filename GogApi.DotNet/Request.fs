module GogApi.DotNet.FSharp.Request

open System.IO

open GogApi.DotNet.FSharp.Base

let rec makeRequest<'T> method auth queries url :'T option * Authentication =
    try
        (makeBasicJsonRequest method auth queries url, auth)
    with
    | ex ->
        match auth with
        | Auth x ->
            match x with
            | { refreshed = true } ->
                printfn "Returned Json is not valid! Refreshing the authentication did not work."
                File.WriteAllText ("log.txt", ex.Message)
                (None, Auth { x with refreshed = false})
            | { refreshed = false } ->
                // Refresh authentication
                let auth' = Authentication.refresh x
                let appData = auth'
                makeRequest<'T> method appData queries url
        | NoAuth ->
            printfn "No authentication was given. Maybe valid authentication is necessary?"
            (None, NoAuth)
