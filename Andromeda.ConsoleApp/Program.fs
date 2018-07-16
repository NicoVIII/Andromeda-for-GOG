// Learn more about F# at http://fsharp.org

open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Games
open Andromeda.Core.FSharp.Installed
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

    let (appData, authenticated) =
        match appData with
        | { authentication = NoAuth } ->
            ({ appData with authentication = authenticate () }, true)
        | { authentication = Auth _ } ->
            (appData, false)

    let auth = appData.authentication
    match (start, auth) with
    | (true, NoAuth) ->
        printfn "Authentication failed!"
        1
    | (true, Auth x) ->
        printfn "Authentication successful!"

        match authenticated with
        | true -> saveAppData appData
        | false -> ()

        getUserData appData
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
            nextRound appData
        | "search-installed" ->
            let appData = searchInstalled appData
            nextRound appData
        | "list-installed" ->
            appData.installedGames
            |> List.iter (fun game -> printfn "%s - %s" game.name game.version)
            nextRound appData
        | "check-updates" ->
            let (updates, appData) = checkAllForUpdates appData
            updates
            |> List.iter (printfn "%A")
            nextRound appData
        | "logout" ->
            printfn "Logged out."
            saveAppData { appData with authentication = NoAuth }
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


    loadAppData ()
    |> mainloop true

    //getOwnedGameIds auth
    //|> List.iter (fun (GameId id) -> printfn "%i" id)
