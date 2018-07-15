// Learn more about F# at http://fsharp.org

open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Games
open Andromeda.Core.FSharp.SaveLoad
open Andromeda.Core.FSharp.User
open Andromeda.ConsoleApp
open Couchbase.Lite
open System
open Couchbase.Lite.Logging

let authenticate () =
    printfn "Please go to https://auth.gog.com/auth?client_id=46899977096215655&redirect_uri=https%%3A%%2F%%2Fembed.gog.com%%2Fon_login_success%%3Forigin%%3Dclient&response_type=code&layout=client2 and log in."
    printfn "Enter Code from url (..code=<code>) here:"

    System.Console.ReadLine ()
    |> sscanf "%s"
    |> Token.newToken

let rec mainloop start auth =
    let newRound () = mainloop true NoAuth
    let nextRound = mainloop false

    let auth =
        match auth with
        | NoAuth -> authenticate ()
        | auth -> auth

    match (start, auth) with
    | (true, NoAuth) ->
        printfn "Authentication failed!"
        1
    | (true, Auth _) ->
        printfn "Authentication successful!"

        saveAuth auth

        getUserData auth
        |> fst
        |> function
            | Some r ->
                printfn "Logged in as: %s <%s>" r.username r.email
            | None -> ()
        nextRound auth
    | (false, auth) ->
        printfn "What do you want to do?"
        printf "> "
        let command = Console.ReadLine () |> sscanf "%s"
        match command with
        | "help" ->
            printfn "Available commands:"
            printfn "- logout: Logs you out. You have to reauthenticate after that."
            printfn "- quit: Close Andromeda."
            saveAuth NoAuth
            nextRound auth
        | "logout" ->
            printfn "Logged out."
            newRound ()
        | "exit" | "close" | "quit" ->
            0
        | s ->
            printfn "Command '%s' not found. Type 'help' to get an overview over all available commands!" s
            nextRound auth

[<EntryPoint>]
let main _ =
    // Initialise Couchbase Lite
    Couchbase.Lite.Support.NetDesktop.Activate ()
    Database.SetLogLevel (LogDomain.All, LogLevel.None)

    loadAuth ()
    |> mainloop true

    //getOwnedGameIds auth
    //|> List.iter (fun (GameId id) -> printfn "%i" id)
