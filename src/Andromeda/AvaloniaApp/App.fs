namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Andromeda.Core.Lenses
open Elmish
open GogApi.DomainTypes

open Andromeda.AvaloniaApp.Components

module App =
    type ProgramState =
        | Unauthenticated of Authentication.State
        | Authenticated of Main.State

    type WindowStates =
        { programState: ProgramState
          installGameWindow: InstallGame.InstallGameWindow option
          settingsWindow: Settings.SettingsWindow option }

    type Msg =
        | CloseAllWindows
        | CloseSettingsWindow of Settings.IWindow * Settings
        | CloseInstallGameWindow of ProductInfo * Authentication
        | OpenInstallGameWindow
        | OpenSettingsWindow
        | Authenticate of Authentication
        // Child component messages
        | MainMsg of Main.Msg
        | AuthenticationMsg of Authentication.Msg

    module Subs =
        let closeWindow (wind: IAndromedaWindow) =
            let sub dispatch =
                wind.AddClosedHandler(fun _ -> CloseAllWindows |> dispatch)
                |> ignore

            Cmd.ofSub sub

        let installGameWindow authentication state =
            let sub dispatch =
                match state.installGameWindow with
                | Some window ->
                    let wind = window :> InstallGame.IInstallGameWindow

                    wind.OnSave.Subscribe
                        (fun (_, downloadInfo) ->
                            (downloadInfo, authentication)
                            |> CloseInstallGameWindow
                            |> dispatch)
                    |> ignore
                | None -> ()

            Cmd.ofSub sub

        let saveSettings (window: Settings.IWindow) =
            let sub dispatch =
                window.OnSave.Subscribe
                    (fun (window, settings) ->
                        (window, settings)
                        |> CloseSettingsWindow
                        |> dispatch)
                |> ignore

            Cmd.ofSub sub

    let initAuthenticated authentication =
        let settings = Persistence.Settings.load ()

        let mainState, mainCmd = Main.Model.init settings authentication

        Authenticated mainState, Cmd.map MainMsg mainCmd

    let init authentication: WindowStates * Cmd<Msg> =
        let programState, programCmd =
            match authentication with
            | Some authentication ->
                initAuthenticated authentication
            | None -> Authentication.init () |> Unauthenticated, Cmd.none

        let state =
            { programState = programState
              installGameWindow = None
              settingsWindow = None }

        state, programCmd

    let update msg (state: WindowStates) mainWindow: WindowStates * Cmd<Msg> =
        let idle = state, Cmd.none

        match msg, state.programState with
        | OpenInstallGameWindow, Authenticated mainState ->
            let authentication =
                getl Main.StateL.authentication mainState

            let installedGames =
                getl Main.StateL.installedGames mainState
                |> Map.toList
                |> List.map fst

            let window =
                InstallGame.InstallGameWindow(authentication, installedGames)

            window.ShowDialog(mainWindow) |> ignore

            let state =
                { state with
                      installGameWindow = window |> Some }

            state, Subs.installGameWindow authentication state
        | OpenInstallGameWindow, _ -> idle
        | OpenSettingsWindow, Authenticated mainState ->
            let authentication =
                getl Main.StateL.authentication mainState

            let settings = getl Main.StateL.settings mainState

            let window = Settings.SettingsWindow(settings)

            window.ShowDialog(mainWindow) |> ignore

            let cmd = Subs.saveSettings window

            { state with
                  settingsWindow = window |> Some },
            cmd
        | OpenSettingsWindow, _ -> idle
        | CloseInstallGameWindow (downloadInfo, authentication), _ ->
            let cmd =
                (downloadInfo, authentication)
                |> Main.StartGameDownload
                |> MainMsg
                |> Cmd.ofMsg

            { state with installGameWindow = None }, cmd
        | CloseSettingsWindow (window, settings), _ ->
            window.CloseWithoutCustomHandler()

            let cmd =
                Main.SetSettings settings |> MainMsg |> Cmd.ofMsg

            { state with settingsWindow = None }, cmd
        | CloseAllWindows, _ ->
            let closeWindow (window: IAndromedaWindow) =
                window.CloseWithoutCustomHandler()

            Option.iter closeWindow state.settingsWindow
            closeWindow mainWindow

            state, Cmd.none
        | Authenticate authentication, _ ->
            let programState, programCmd = initAuthenticated authentication
            let state = { state with programState = programState }

            state, programCmd
        // Update child components
        | MainMsg msg, Authenticated mainState ->
            let mainState, mainCmd, intent = Main.Update.update msg mainState

            let state =
                { state with
                      programState = Authenticated mainState }

            let intentCmd =
                match intent with
                | Main.DoNothing -> Cmd.none
                | Main.OpenInstallGameWindow -> Cmd.ofMsg OpenInstallGameWindow
                | Main.OpenSettings -> Cmd.ofMsg OpenSettingsWindow

            let cmd =
                [ Cmd.map MainMsg mainCmd; intentCmd ]
                |> Cmd.batch

            state, cmd
        | MainMsg _, _ -> idle
        | AuthenticationMsg msg, Unauthenticated authState ->
            let authState, authCmd, intent = Authentication.update msg authState

            let state =
                { state with
                      programState = Unauthenticated authState }

            let intentCmd =
                match intent with
                | Authentication.DoNothing -> Cmd.none
                | Authentication.Authenticate authentication ->
                    Persistence.Authentication.save authentication
                    |> ignore

                    authentication
                    |> Authenticate
                    |> Cmd.ofMsg

            let cmd =
                [ Cmd.map AuthenticationMsg authCmd
                  intentCmd ]
                |> Cmd.batch

            state, cmd
        | AuthenticationMsg _, _ -> idle

    let render (state: WindowStates) dispatch =
        match state.programState with
        | Authenticated state -> Main.View.render state (MainMsg >> dispatch)
        | Unauthenticated state ->
            Authentication.render state (AuthenticationMsg >> dispatch)
