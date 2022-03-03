namespace Andromeda.AvaloniaApp

open Andromeda.Core.DomainTypes
open GogApi.DomainTypes

open Andromeda.AvaloniaApp.Components

type AuthMsg =
    | StartGame of InstalledGame
    | UpgradeGame of InstalledGame
    | SetGameImage of ProductId * string
    | AddNotification of string
    | RemoveNotification of string
    | AddToTerminalOutput of string
    | SetSettings of Settings
    | SearchInstalled of initial: bool
    | StartGameDownload of ProductInfo * Dlc list * Authentication
    | UnpackGame of Settings * DownloadStatus * version: string option
    | FinishGameDownload of ProductId
    | UpdateDownloadSize of ProductId * int
    | UpdateDownloadInstalling of ProductId
    | UpgradeGames
    | CacheCheck
    // Context change
    | ShowInstallGame
    | ShowSettings
    | ShowInstalled
    // Child component messages
    | InstallGameMgs of InstallGame.Msg
    | SettingsMsg of Settings.Msg

type UnAuthMsg =
    | Authenticate of Authentication
    // Child component messages
    | AuthenticationMsg of Authentication.Msg

type Msg =
    | Auth of AuthMsg
    | UnAuth of UnAuthMsg
    | CloseAllWindows
