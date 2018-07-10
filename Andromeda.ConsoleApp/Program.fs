// Learn more about F# at http://fsharp.org

open Andromeda.Core.FSharp.Auth
open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Games
open Andromeda.ConsoleApp
open System

[<EntryPoint>]
let main argv =
    printfn "Please go to https://auth.gog.com/auth?client_id=46899977096215655&redirect_uri=https%%3A%%2F%%2Fembed.gog.com%%2Fon_login_success%%3Forigin%%3Dclient&response_type=code&layout=client2 and log in."
    printfn "Enter Code from url (..code=<code>) here:"

    let auth :Authentication =
        System.Console.ReadLine ()
        |> sscanf "%s"
        |> newToken
        |> function
            | None -> Empty
            | Some { access_token = token } ->
                token
                |> createAuth

    match auth with
    | Empty ->
        printfn "Authentication failed!"
    | Auth _ ->
        printfn "Authentication successful!"
        getOwnedGameIds auth
        |> List.iter (fun (GameId id) -> printfn "%i" id)

    0 // return an integer exit code
