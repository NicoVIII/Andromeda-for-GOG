module GogApi.DotNet.FSharp.Base

open FSharp.Json
open Hopac
open HttpFs.Client

type QueryString = {
    name: QueryStringName;
    value: QueryStringValue;
}

let createQuery name value = { name = name; value = value }

type AuthenticationData = {
    accessToken: string;
    refreshToken: string;
    refreshed: bool;
}

type Authentication = NoAuth | Auth of AuthenticationData

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

let makeBasicJsonRequest<'T> method auth queries url :'T option =
    setupBasicRequest method auth queries url
    |> Request.responseAsString
    |> run
    |> Json.deserializeEx<'T> config
    |> Some
