namespace Andromeda.AvaloniaApp.Components.Main

open Andromeda.AvaloniaApp
open Andromeda.Core
open Andromeda.Core.DomainTypes
open Andromeda.Core.Lenses
open Elmish
open GogApi.DotNet.FSharp.DomainTypes

type State =
    { authentication: Authentication
      downloads: Map<ProductId, DownloadStatus>
      installedGames: Map<ProductId, InstalledGame>
      mode: Mode
      notifications: string list
      settings: Settings
      terminalOutput: string list }

/// Lenses to simplify usage of state
module StateL =
    let authentication =
        Lens((fun r -> r.authentication), (fun r v -> { r with authentication = v }))

    let downloads =
        Lens((fun r -> r.downloads), (fun r v -> { r with downloads = v }))

    let installedGames =
        Lens((fun r -> r.installedGames), (fun r v -> { r with installedGames = v }))

    let mode =
        Lens((fun r -> r.mode), (fun r v -> { r with mode = v }))

    let notifications =
        Lens((fun r -> r.notifications), (fun r v -> { r with notifications = v }))

    let settings =
        Lens((fun r -> r.settings), (fun r v -> { r with settings = v }))

    let terminalOutput =
        Lens((fun r -> r.terminalOutput), (fun r v -> { r with terminalOutput = v }))

type Intent =
    | DoNothing
    | OpenSettings
    | OpenInstallGameWindow

type Msg =
    | ChangeState of (State -> State)
    | ChangeMode of Mode
    | StartGame of InstalledGame
    | UpgradeGame of InstalledGame
    | SetGameImage of ProductId * string
    | AddNotification of string
    | RemoveNotification of string
    | AddToTerminalOutput of string
    | SetSettings of Settings
    | SearchInstalled of initial: bool
    | StartGameDownload of ProductInfo * Authentication
    | UnpackGame of Settings * DownloadStatus * version: string option
    | FinishGameDownload of ProductId
    | UpdateDownloadSize of ProductId * int
    | UpdateDownloadInstalling of ProductId
    | UpgradeGames
    | CacheCheck
    // Intent messages
    | OpenSettings
    | OpenInstallGameWindow

module Model =
    let init (settings: Settings option) (authentication: Authentication) =
        let settings =
            settings
            |> Option.defaultValue (SystemInfo.defaultSettings ())

        let state =
            { authentication = authentication
              downloads = Map.empty
              installedGames = Map.empty
              mode = Installed
              notifications = []
              settings = settings
              terminalOutput = [] }

        let cmd =
            // We initialy search for installed games and perform a cache check
            [ CacheCheck; SearchInstalled true ]
            |> List.map Cmd.ofMsg
            |> Cmd.batch

        state, cmd
