module Andromeda.Core.FSharp.Basics

open FSharp.Json
open Hopac
open HttpFs.Client
open System.IO

open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Responses

type QueryString = {
    name: QueryStringName;
    value: QueryStringValue;
}

let redirectUri = "https://embed.gog.com/on_login_success?origin=client"
let config = JsonConfig.create(allowUntyped = true)

let setupBasicRequest method auth queries url =
    (Request.createUrl method url, auth)
    // Add auth data
    |> function
        | (r, NoAuth) ->
            r
        | (r, Auth {accessToken = token}) ->
            Request.setHeader (Authorization ("Bearer " + token)) r
    // Add query parameters
    |> List.fold (fun request query -> Request.queryStringItem query.name query.value request) <| queries

(*let makeBasicFileRequest method auth queries url filepath =
    job {
        use! resp =
            setupBasicRequest method auth queries url
            |> getResponse
        use fileStream = new FileStream(filepath, FileMode.Create)
        do! resp.body.CopyToAsync fileStream
    }*)

let makeBasicJsonRequest<'T> method auth queries url :'T option =
    setupBasicRequest method auth queries url
    |> Request.responseAsString
    |> run
    |> Json.deserializeEx<'T> config
    |> Some

let createQuery name value = { name = name; value = value }

module Token =
    let createAuth refreshed response =
        match response with
        | Some response ->
            Auth { accessToken = response.access_token; refreshToken = response.refresh_token; refreshed = refreshed }
        | None ->
            NoAuth

    let getBasicQueries () =
        [
            createQuery "client_id" "46899977096215655";
            createQuery "client_secret" "9d85c43b1482497dbbce61f6e4aa173a433796eeae2ca8c5f6129f2dc4de46d9";
        ]

    let newToken (code :string) =
        getBasicQueries ()
        |> List.append [ createQuery "grant_type" "authorization_code" ]
        |> List.append [ createQuery "code" code ]
        |> List.append [ createQuery "redirect_uri" redirectUri ]
        |> makeBasicJsonRequest<TokenResponse> Get NoAuth <| "https://auth.gog.com/token"
        |> createAuth false

    let refresh auth =
        getBasicQueries ()
        |> List.append [ createQuery "grant_type" "refresh_token" ]
        |> List.append [ createQuery "refresh_token" auth.refreshToken ]
        |> makeBasicJsonRequest<TokenResponse> Get NoAuth <| "https://auth.gog.com/token"
        |> createAuth true

let rec makeRequest<'T> method appData queries url :'T option * AppData =
    let auth = appData.authentication
    try
        (makeBasicJsonRequest method auth queries url, appData)
    with
    | ex ->
        match auth with
        | Auth x ->
            match x with
            | { refreshed = true } ->
                printfn "Returned Json is not valid! Refreshing the authentication did not work."
                File.WriteAllText ("log.txt", ex.Message)
                (None, { appData with authentication = Auth { x with refreshed = false}})
            | { refreshed = false } ->
                // Refresh authentication
                let auth' = Token.refresh x
                let appData = { appData with authentication = auth' }
                saveAppData appData
                makeRequest<'T> method appData queries url
        | NoAuth ->
            printfn "No authentication was given. Maybe valid authentication is necessary?"
            (None, { appData with authentication = NoAuth })
