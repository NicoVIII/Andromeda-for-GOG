namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Elmish

open Andromeda.AvaloniaApp.Components

[<RequireQualifiedAccess>]
module Init =
    let authenticated authentication =
        let settings = Persistence.Settings.load ()

        let state, cmd = Main.Model.init settings authentication

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
