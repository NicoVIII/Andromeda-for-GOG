namespace Andromeda.AvaloniaApp

open GogApi.DomainTypes

open Andromeda.AvaloniaApp.Components

type AuthMsg =
    | OpenInstallGameWindow
    | CloseInstallGameWindow of ProductInfo * Dlc list * Authentication
    | OpenSettings
    | ResetContext
    // Child component messages
    | MainMsg of Main.Msg
    | SettingsMsg of Settings.Msg

type UnAuthMsg =
    | Authenticate of Authentication
    // Child component messages
    | AuthenticationMsg of Authentication.Msg

type Msg =
    | Auth of AuthMsg
    | UnAuth of UnAuthMsg
    | CloseAllWindows
