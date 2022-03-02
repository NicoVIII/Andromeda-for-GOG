namespace Andromeda.AvaloniaApp

open Andromeda.AvaloniaApp.Components

/// Special contexts the application can be in which possibly handle their own
/// state additionally to the global one
type Context =
    | Installed
    | Settings of Settings.State

type AuthenticatedState =
    { main: Main.State
      installGameWindow: InstallGame.InstallGameWindow option
      context: Context }

type State =
    | Unauthenticated of Authentication.State
    | Authenticated of AuthenticatedState
