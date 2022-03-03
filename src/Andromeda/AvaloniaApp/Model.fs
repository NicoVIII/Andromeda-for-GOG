namespace Andromeda.AvaloniaApp

open Andromeda.Core.DomainTypes
open GogApi.DomainTypes

open Andromeda.AvaloniaApp
open Andromeda.AvaloniaApp.Components

/// Special contexts the application can be in which possibly handle their own
/// state additionally to the global one
type Context =
    | Installed
    | Settings of Settings.State

type MainState =
    { authentication: Authentication
      downloads: Map<ProductId, DownloadStatus>
      installedGames: Map<ProductId, InstalledGame>
      notifications: string list
      settings: Settings
      terminalOutput: string list }


type AuthenticatedState =
    { main: MainState
      installGameWindow: InstallGame.InstallGameWindow option
      context: Context }

type State =
    | Unauthenticated of Authentication.State
    | Authenticated of AuthenticatedState
