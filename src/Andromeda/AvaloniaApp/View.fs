namespace Andromeda.AvaloniaApp

open Andromeda.AvaloniaApp.Components

module View =
    let renderAuthenticated state dispatch =
        match state.context with
        | Main -> Main.View.render state.main (MainMsg >> dispatch)
        | Settings state -> Settings.View.render state (SettingsMsg >> dispatch)

    let render state dispatch =
        match state with
        | Authenticated state -> renderAuthenticated state (Auth >> dispatch)
        | Unauthenticated state ->
            Authentication.render state (AuthenticationMsg >> UnAuth >> dispatch)
