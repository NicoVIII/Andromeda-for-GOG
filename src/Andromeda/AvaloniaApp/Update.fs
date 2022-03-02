namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Elmish
open SimpleOptics

open Andromeda.AvaloniaApp.Components
open Andromeda.AvaloniaApp.DomainTypes

module Update =
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

                    wind.OnSave.Subscribe (fun (_, downloadInfo, dlcs) ->
                        (downloadInfo, dlcs, authentication)
                        |> CloseInstallGameWindow
                        |> dispatch)
                    |> ignore
                | None -> ()

            Cmd.ofSub sub

    let performAuthenticated msg state mainWindow =
        match msg with
        | OpenInstallGameWindow ->
            let authentication = Optic.get Main.StateL.authentication state.main

            let installedGames =
                Optic.get Main.StateL.installedGames state.main
                |> Map.toList
                |> List.map fst

            let window = InstallGame.InstallGameWindow(authentication, installedGames)

            window.ShowDialog(mainWindow) |> ignore

            let state = { state with installGameWindow = window |> Some }

            state, Subs.installGameWindow authentication state
        | CloseInstallGameWindow (downloadInfo, dlcs, authentication) ->
            let cmd =
                (downloadInfo, dlcs, authentication)
                |> Main.StartGameDownload
                |> MainMsg
                |> Cmd.ofMsg

            { state with installGameWindow = None }, cmd
        | OpenSettings ->
            let settings, cmd = Settings.init state.main.settings

            { state with context = Settings settings }, cmd
        | ShowInstalled -> { state with context = Installed }, Cmd.none
        // Child components
        | MainMsg msg ->
            let mainState, mainCmd, intent = Main.Update.update msg state.main

            let state = { state with main = mainState }

            let intentCmd =
                match intent with
                | Main.DoNothing -> Cmd.none
                | Main.OpenInstallGameWindow -> Cmd.ofMsg OpenInstallGameWindow
                | Main.OpenSettings -> Cmd.ofMsg OpenSettings

            let cmd =
                [ Cmd.map MainMsg mainCmd; intentCmd ]
                |> Cmd.batch

            state, cmd
        | SettingsMsg msg ->
            let settingsState, settingsCmd, intent =
                Settings.update mainWindow msg state.main.settings

            let state = { state with main = { state.main with settings = settingsState } }

            let intentCmd =
                match intent with
                | Settings.DoNothing -> Cmd.none
                | Settings.Save settings ->
                    [ Main.SetSettings settings |> MainMsg
                      ShowInstalled ]
                    |> List.map Cmd.ofMsg
                    |> Cmd.batch

            let cmd =
                [ Cmd.map SettingsMsg settingsCmd
                  intentCmd ]
                |> Cmd.batch

            state, cmd

    let performUnauthenticated msg state =
        match msg with
        | Authenticate authentication ->
            let state, programCmd = Init.authenticated authentication

            state, Cmd.map Auth programCmd
        | AuthenticationMsg msg ->
            let state, authCmd, intent = Authentication.update msg state

            let intentCmd =
                match intent with
                | Authentication.DoNothing -> Cmd.none
                | Authentication.Authenticate authentication ->
                    Persistence.Authentication.save authentication
                    |> ignore

                    authentication |> Authenticate |> Cmd.ofMsg

            let cmd =
                [ Cmd.map AuthenticationMsg authCmd
                  intentCmd ]
                |> Cmd.batch

            Unauthenticated state, Cmd.map UnAuth cmd

    let perform msg (state: State) mainWindow =
        match msg, state with
        | Auth msg, Authenticated state ->
            let state, cmd = performAuthenticated msg state mainWindow
            Authenticated state, Cmd.map Auth cmd
        | Auth _, Unauthenticated _ -> failwith "Got unauthenticated msg in auth state"
        | UnAuth msg, Unauthenticated state -> performUnauthenticated msg state
        | UnAuth _, Authenticated _ -> failwith "Got authenticated msg in unauth state"
        | CloseAllWindows, _ ->
            let closeWindow (window: IAndromedaWindow) =
                window.CloseWithoutCustomHandler()

            closeWindow mainWindow

            state, Cmd.none
