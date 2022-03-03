namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Elmish
open GogApi.DomainTypes

open Andromeda.AvaloniaApp.Components

[<RequireQualifiedAccess>]
module Init =
    let main (settings: Settings option) (authentication: Authentication) =
        let settings =
            settings
            |> Option.defaultValue (SystemInfo.defaultSettings ())

        let state =
            { authentication = authentication
              downloads = Map.empty
              installedGames = Map.empty
              notifications = []
              settings = settings
              terminalOutput = [] }

        let cmd =
            // We initialy search for installed games and perform a cache check
            [ CacheCheck; SearchInstalled true ]
            |> List.map Cmd.ofMsg
            |> Cmd.batch

        state, cmd

    let authenticated authentication =
        let settings = Persistence.Settings.load ()

        let state, cmd = main settings authentication

        Authenticated
            { main = state
              context = Installed
              installGameWindow = None },
        Cmd.map MainMsg cmd

    let perform authentication =
        match authentication with
        | Some authentication ->
            let state, cmd = authenticated authentication

            state, Cmd.map Auth cmd
        | None -> Authentication.init () |> Unauthenticated, Cmd.none
