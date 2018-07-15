// Learn more about F# at http://fsharp.org

open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Games
open Andromeda.Core.FSharp.Installed
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

let rec mainloop start appData =
    let newRound () = mainloop true { authentication = NoAuth; installedGames = appData.installedGames }
    let nextRound = mainloop false

    let appData =
        match appData with
        | { authentication = NoAuth } -> { appData with authentication = authenticate () }
        | { authentication = Auth _ } -> appData

    let auth = appData.authentication
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
        nextRound appData
    | (false, auth) ->
        printfn "\nWhat do you want to do?"
        printf "> "
        let command = Console.ReadLine () |> sscanf "%s"
        match command with
        | "help" ->
            printfn "Available commands:"
            printfn "- logout: Logs you out. You have to reauthenticate after that."
            printfn "- quit: Close Andromeda."
            saveAuth NoAuth
            nextRound appData
        | "search-installed" ->
            let installed = searchInstalled ()
            nextRound { appData with installedGames = installed }
        | "list-installed" ->
            appData.installedGames
            |> List.iter (printfn "%A")
            nextRound appData
        | "logout" ->
            printfn "Logged out."
            newRound ()
        | "exit" | "close" | "quit" ->
            0
        | s ->
            printfn "Command '%s' not found. Type 'help' to get an overview over all available commands!" s
            nextRound appData

[<EntryPoint>]
let main _ =
    printfn "Andromeda for GOG - v0.1.0"

    // Initialise Couchbase Lite
    Couchbase.Lite.Support.NetDesktop.Activate ()
    Database.SetLogLevel (LogDomain.All, LogLevel.None)

    { authentication = loadAuth (); installedGames = loadInstalledGames () }
    |> mainloop true

    //getOwnedGameIds auth
    //|> List.iter (fun (GameId id) -> printfn "%i" id)
