namespace Andromeda.AvaloniaApp

open GogApi.DomainTypes

open Andromeda.Core.DomainTypes
open Andromeda.Core.Installed

open Andromeda.AvaloniaApp.Components

type AuthMsg =
    | StartGame of gameId: ProductId * gameDir: string
    | UpgradeGame of Game * showNotification: bool
    | FinishGameUpgrade of
        Game *
        showNotification: bool *
        UpdateData option *
        Authentication
    | LookupGameImage of ProductId
    | SetGameImage of ProductId * string
    | AddNotification of string
    | RemoveNotification of string
    | AddToTerminalOutput of ProductId * string
    | SetSettings of Settings
    | SearchInstalled of initial: bool
    | StartGameDownload of ProductInfo * Dlc list * Authentication
    | UnpackGame of
        Settings *
        Game *
        filepath: string *
        checksum: string option *
        version: string option
    | FinishGameDownload of ProductId * gameDir: string * version: string option
    | UpdateDownloadSize of ProductId * int<MiB>
    | UpdateDownloadInstalling of ProductId
    | UpgradeGames of showNotifications: bool
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
