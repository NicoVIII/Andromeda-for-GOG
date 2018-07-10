module Andromeda.Core.FSharp.Basics

open Hopac
open HttpFs.Client
open FSharp.Json

type AuthenticationData = {
    token: string
}

type Authentication = Empty | Auth of AuthenticationData

type QueryString = {
    name: QueryStringName;
    value: QueryStringValue;
}

let createQuery name value = { name = name; value = value }

let createAuth token = Auth { token = token }

let makeRequest<'T> method auth queries url :'T option =
    let result =
        (Request.createUrl method url, auth)
        // Add auth data
        |> function
            | (r, Empty) ->
                r
            | (r, Auth {token = token}) ->
                Request.setHeader (Authorization ("Bearer " + token)) r
        // Add query parameters
        |> List.fold (fun request query -> Request.queryStringItem query.name query.value request) <| queries
        |> Request.responseAsString
        |> run

    try
        result
        |> Json.deserialize<'T>
        |> Some
    with
    | :? System.Exception ->
        printfn "Json is not valid!\n%s" result
        use file = System.IO.File.AppendText("log.txt");
        fprintfn file "Json is not valid!\n%s" result
        System.IO.File.WriteAllText("lasterror.html", result)
        None
