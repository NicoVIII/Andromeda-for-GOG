namespace Andromeda.AvaloniaApp

open GogApi.DomainTypes

open Andromeda.Core
open Andromeda.Core.Installed

open Andromeda.AvaloniaApp.Components

type ContextChangeMsg =
    | ShowInstallGame
    | ShowSettings
    | ShowInstalled

type AuthMsg =
    | StartGame of gameId: ProductId * gameDir: string
    | LookupGameImage of ProductId
    | SetGameImage of ProductId * string
    | AddNotification of string
    | RemoveNotification of string
    | AddToTerminalOutput of ProductId * string
    | SetSettings of Settings
    | SearchInstalled of initial: bool
    | CacheCheck
    // Feature: Install game
    | SearchGameDownload of ProductInfo * Dlc list * Authentication
    | StartGameDownload of ProductInfo * InstallerInfo list
    | SetupGameDownloadMonitoring of
        ProductInfo *
        InstallerInfo *
        Download.GameDownload option
    | UpdateDownloadSize of ProductId * int<MiB>
    | UpdateDownloadInstalling of ProductId
    | FinishGameDownload of ProductId * gameDir: string * version: string option
    | UnpackGame of
        Settings *
        Game *
        filepath: string *
        checksum: string option *
        version: string option
    // Feature: Upgrade game
    | UpgradeGame of Game * showNotification: bool
    | FinishGameUpgrade of
        Game *
        showNotification: bool *
        UpdateData option *
        Authentication
    | UpgradeGames of showNotifications: bool
    // Feature: Context
    | ContextChangeMsg of ContextChangeMsg
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
