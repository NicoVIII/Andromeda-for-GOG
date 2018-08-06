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
open Andromeda.Core.FSharp.Responses

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
    | (true, Auth _) ->
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
    | (false, _) ->
        printfn "\nWhat do you want to do?"
        printf "> "
        let input =
            Console.ReadLine ()
            |> String.split ' '
            |> function
               | command::arg::lst -> (command, Some (List.fold (fun out s -> sprintf "%s %s" out s) "" (arg::lst)))
               | [command] -> (command, None)
               | [] -> ("", None)
        match input with
        | ("help", None) ->
            printfn "Available commands:"
            printfn "- search-installed: Searches the default location for installed GOG games (only on linux for now)"
            printfn "- list-installed: Lists the installed and found GOG games. Run 'search-installed', if empty."
            printfn "- check-updates: Looks for which games GOG has a newer version online."
            printfn "- install <name>: Trys to download and install a game by name. You can use spaces. Don't use \" for now. (Alpha)"
            printfn ""
            printfn "- logout: Logs you out. You have to reauthenticate after that."
            printfn "- quit: Close Andromeda."
            nextRound appData
        | ("install", Some arg) ->
            let (games, appData) = getAvailableGamesForSearch appData arg
            let appData =
                match games with
                | None ->
                    printfn "No games found for search: %s" arg
                    appData
                | Some games ->
                    let game =
                        match games with
                        | [game] ->
                            printfn "Found \"%s\"" game.title
                            game
                        | games ->
                            printfn "Please choose a game:"
                            List.iteri (fun index (game :ProductInfo) -> printfn "%i: %s" index game.title) games
                            let index = sscanf "%i" (Console.ReadLine ())
                            games.[index]
                    let (installers, appData) = getAvailableInstallersForOs appData game
                    match installers with
                    | [] ->
                        printfn "No installer for your os found. Sorry!"
                        appData
                    | installers ->
                        let installer =
                            match installers with
                            | [installer] -> installer
                            | lst ->
                                printfn "Please choose an installer:"
                                List.head installers
                        let (res, appData) = downloadGame appData installer
                        match res with
                        | true ->
                            searchInstalled appData
                        | false ->
                            printfn "Game could not be installed. Reason unknown."
                            appData
            nextRound appData
        | ("search-installed", None) ->
            let appData = searchInstalled appData
            nextRound appData
        | ("list-installed", None) ->
            appData.installedGames
            |> List.iter (fun game ->
                let (Version version) = game.version
                let updates =
                    match game.updateable with
                    | true -> ""
                    | false -> " (not updateable)"
                printfn "%s - %s%s" game.name version updates
            )
            nextRound appData
        | ("check-updates", None) ->
            let (updates, appData) = checkAllForUpdates appData
            match updates with
            | updates when updates.Length > 0 ->
                List.iter (fun update ->
                    let (Version oldVersion) = update.game.version
                    let (Version newVersion) = update.newVersion
                    printfn "There is another version of '%s' available: %s -> %s" update.game.name oldVersion newVersion
                ) updates
            | _ ->
                printfn "No updates available."

            let notUpdateable = appData.installedGames |> List.filter (fun game -> game.updateable |> not) |> List.length
            match notUpdateable with
            | 0 ->
                ()
            | x ->
                printfn "\n%i games are not updateable." x
            nextRound appData
        | ("logout", None) ->
            printfn "Logged out."
            saveAppData { appData with authentication = NoAuth }
            newRound ()
        | ("exit", None) | ("close", None) | ("quit", None) ->
            0
        | (s, _) ->
            printfn "Command '%s' not found. Type 'help' to get an overview over all available commands!" s
            nextRound appData

[<EntryPoint>]
let main _ =
    printfn "Andromeda for GOG - v0.2.0-alpha.1"

    // Initialise Couchbase Lite
    Couchbase.Lite.Support.NetDesktop.Activate ()
    Database.SetLogLevel (LogDomain.All, LogLevel.None)

    loadAppData ()
    |> mainloop true
