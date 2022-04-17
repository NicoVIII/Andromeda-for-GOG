namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Elmish

open Andromeda.AvaloniaApp.Components

[<RequireQualifiedAccess>]
module Init =
    let authenticated authentication =
        let settings =
            Persistence.Settings.load ()
            |> Option.defaultValue (SystemInfo.defaultSettings ())

        let state =
            Authenticated
                { authentication = authentication
                  games = Map.empty
                  notifications = []
                  settings = settings
                  terminalOutput = Map.empty
                  context = Installed }

        let cmd =
            // We initialy search for installed games and perform a cache check
            [ CacheCheck; SearchInstalled true ]
            |> List.map Cmd.ofMsg
            |> Cmd.batch

        state, cmd

    let perform authentication =
        match authentication with
        | Some authentication ->
            let state, cmd = authenticated authentication

            state, Cmd.map Auth cmd
        | None -> Authentication.init () |> Unauthenticated, Cmd.none
