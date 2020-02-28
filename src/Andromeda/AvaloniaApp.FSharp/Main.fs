namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Platform
open Avalonia.Threading
open Elmish
open GogApi.DotNet.FSharp.Listing
open MessageBox.Avalonia
open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Threading.Tasks

module Main =
    let createDownloadStatus id gameTitle filePath fileSize =
        { DownloadStatus.gameId = id
          gameTitle = gameTitle
          filePath = filePath
          fileSize = fileSize
          downloaded = 0
          installing = false }

    let gameToDownloadInfo (game: InstalledGame) =
        { ProductInfo.id = game.id
          title = game.name }

    type State =
        { leftBarState: LeftBar.State
          authentication: Authentication
          authenticationWindow: Authentication.AuthenticationWindow option
          downloads: DownloadStatus list
          installGameWindow: InstallGame.InstallGameWindow option
          installedGames: InstalledGame list
          notifications: string list
          settings: Settings option
          settingsWindow: Settings.SettingsWindow option
          terminalOutput: string
          window: HostWindow }

    type Msg =
        | LeftBarMsg of LeftBar.Msg
        | AddNotification of string
        | RemoveNotification of string
        | AddToTerminalOutput of string
        | OpenAuthenticationWindow
        | OpenInstallGameWindow
        | OpenSettingsWindow of bool
        | CloseMainWindow
        | CloseAuthenticationWindow of Authentication
        | CloseInstallGameWindow of ProductInfo
        | CloseSettingsWindow of Settings
        | SearchInstalled of Settings
        | SetSettings of Settings
        | StartGame of InstalledGame
        | StartGameDownload of ProductInfo
        | UnpackGame of Tuple<Settings, DownloadStatus>
        | FinishGameDownload of Tuple<string, Settings>
        | UpdateDownloadSize of Tuple<string, int>
        | UpdateDownloadInstalling of string
        | UpgradeGames

    module Subs =
        let closeWindow (window: Window) =
            let sub dispatch = window.Closing.Subscribe(fun _ -> CloseMainWindow |> dispatch) |> ignore
            Cmd.ofSub sub

        let startGame (game: InstalledGame) =
            let sub dispatch =
                let showGameOutput _ (outLine: DataReceivedEventArgs) =
                    if outLine.Data
                       |> String.IsNullOrEmpty
                       |> not then
                        AvaloniaScheduler.Instance.Schedule
                            (outLine.Data,
                             (fun _ newLine ->
                                 AddToTerminalOutput newLine |> dispatch
                                 { new IDisposable with
                                     member __.Dispose() = () }))
                        |> ignore
                    else
                        ()
                Games.startGameProcess showGameOutput game.path |> ignore
            Cmd.ofSub sub

        let registerDownloadTimer (task: Task option) tmppath (downloadInfo: DownloadStatus) =
            let sub dispatch =
                match task with
                | Some _ ->
                    "Download installer for " + downloadInfo.gameTitle + "." |> Logger.LogInfo
                    let invoke() =
                        let fileSize =
                            if File.Exists tmppath then
                                let fileSize =
                                    tmppath
                                    |> FileInfo
                                    |> fun fileInfo -> int (float (fileInfo.Length) / 1000000.0)
                                UpdateDownloadSize(tmppath, fileSize) |> dispatch
                                fileSize
                            else
                                0
                        File.Exists tmppath && fileSize < int downloadInfo.fileSize

                    DispatcherTimer.Run(Func<bool>(invoke), TimeSpan.FromSeconds 0.5) |> ignore
                | None ->
                    "Use cached installer for " + downloadInfo.gameTitle + "." |> Logger.LogInfo
                    UpdateDownloadSize(tmppath, int downloadInfo.fileSize) |> dispatch
            Cmd.ofSub sub

        let removeNotification notification =
            let sub dispatch =
                let invoke() = RemoveNotification notification |> dispatch

                DispatcherTimer.RunOnce(Action(invoke), TimeSpan.FromSeconds 5.0) |> ignore
            Cmd.ofSub sub

        let saveAuthentication state =
            let sub dispatch =
                match state.authenticationWindow with
                | Some window ->
                    let wind = window :> Authentication.IAuthenticationWindow
                    wind.OnSave.Subscribe(fun (_, authentication) ->
                        CloseAuthenticationWindow authentication |> dispatch) |> ignore
                | None -> ()
            Cmd.ofSub sub

        let installGameWindow state =
            let sub dispatch =
                match state.installGameWindow with
                | Some window ->
                    let wind = window :> InstallGame.IInstallGameWindow
                    wind.OnSave.Subscribe(fun (_, downloadInfo) ->
                        downloadInfo
                        |> CloseInstallGameWindow
                        |> dispatch)
                    |> ignore
                | None -> ()
            Cmd.ofSub sub

        let saveSettings state =
            let sub dispatch =
                match state.settingsWindow with
                | Some window ->
                    let wind = window :> Settings.ISettingsWindow
                    wind.OnSave.Subscribe(fun (_, settings) -> CloseSettingsWindow settings |> dispatch) |> ignore
                | None -> ()
            Cmd.ofSub sub

    let init (settings: Settings option, authentication: Authentication, window: HostWindow) =
        let authentication = Authentication.refresh authentication |> Async.RunSynchronously

        AuthenticationPersistence.save authentication |> ignore

        let state =
            { leftBarState = LeftBar.init()
              authentication = authentication
              authenticationWindow = None
              downloads = []
              installedGames = []
              installGameWindow = None
              notifications = []
              settings = None
              settingsWindow = None
              terminalOutput = ""
              window = window }

        let authenticationCommand =
            match authentication with
            | Authentication.Auth _ -> Cmd.none
            | Authentication.NoAuth -> Cmd.ofMsg OpenAuthenticationWindow

        let settingsCommand =
            match settings with
            | Some settings -> SetSettings settings |> Cmd.ofMsg
            | None -> Cmd.ofMsg <| OpenSettingsWindow true

        state,
        Cmd.batch
            [ authenticationCommand
              settingsCommand
              Subs.closeWindow window ]

    let cancelClosingEventHandler =
        new EventHandler<CancelEventArgs>(fun _ event ->
        let info =
            MessageBoxManager.GetMessageBoxStandardWindow
                ("Required", "Please fill this out, it is required... To exit this, close main window.")
        info.Show() |> ignore
        event.Cancel <- true)

    let closeWindow<'T when 'T :> Window> (window: 'T option) =
        match window with
        | Some window ->
            window.Closing.RemoveHandler cancelClosingEventHandler
            window.Close()
        | None -> ()

    let update (msg: Msg) (state: State) =
        match msg with
        | CloseMainWindow ->
            closeWindow state.authenticationWindow
            closeWindow state.settingsWindow
            state, Cmd.none
        | LeftBarMsg leftBarMsg ->
            let (s, cmd) = LeftBar.update leftBarMsg state.leftBarState
            { state with leftBarState = s }, Cmd.map LeftBarMsg cmd
        | AddNotification notification ->
            { state with notifications = notification :: state.notifications }, Subs.removeNotification notification
        | RemoveNotification notification ->
            let notifications = state.notifications |> List.filter (fun n -> n <> notification)
            { state with notifications = notifications }, Cmd.none
        | AddToTerminalOutput newLine ->
            let terminalOutput =
                match state.terminalOutput with
                | "" -> newLine
                | _ -> newLine + Environment.NewLine + state.terminalOutput
            { state with terminalOutput = terminalOutput }, Cmd.none
        | OpenAuthenticationWindow ->
            let window = Authentication.AuthenticationWindow()
            window.Closing.AddHandler cancelClosingEventHandler
            window.ShowDialog(state.window) |> ignore
            let state = { state with authenticationWindow = window |> Some }
            state, Subs.saveAuthentication state
        | OpenInstallGameWindow ->
            let window = InstallGame.InstallGameWindow(state.authentication)
            window.ShowDialog(state.window) |> ignore
            let state = { state with installGameWindow = window |> Some }
            state, Subs.installGameWindow state
        | OpenSettingsWindow initial ->
            let window = Settings.SettingsWindow(state.settings)
            if initial
            then window.Closing.AddHandler cancelClosingEventHandler
            else ()
            window.ShowDialog(state.window) |> ignore
            let state = { state with settingsWindow = window |> Some }
            state, Subs.saveSettings state
        | CloseAuthenticationWindow authentication ->
            AuthenticationPersistence.save authentication |> ignore

            match state.authenticationWindow with
            | Some window -> window.Closing.RemoveHandler cancelClosingEventHandler
            | None -> ()

            { state with
                  authenticationWindow = None
                  authentication = authentication }, Cmd.none
        | CloseInstallGameWindow downloadInfo ->
            { state with installGameWindow = None }, Cmd.ofMsg <| StartGameDownload downloadInfo
        | CloseSettingsWindow settings ->
            match state.settingsWindow with
            | Some window -> window.Closing.RemoveHandler cancelClosingEventHandler
            | None -> ()

            { state with settingsWindow = None }, Cmd.ofMsg (SetSettings settings)
        | SearchInstalled settings ->
            let installedGames = Installed.searchInstalled settings state.authentication
            { state with installedGames = installedGames }, Cmd.none
        | SetSettings settings ->
            SettingsPersistence.save settings |> ignore
            { state with settings = Some settings }, Cmd.ofMsg (settings |> SearchInstalled)
        | FinishGameDownload(filePath, settings) ->
            let statusList = state.downloads |> List.filter (fun ds -> ds.filePath <> filePath)
            { state with downloads = statusList }, Cmd.ofMsg (settings |> SearchInstalled)
        | StartGame installedGame -> state, Subs.startGame installedGame
        | StartGameDownload productInfo ->
            let installerInfoList =
                Games.getAvailableInstallersForOs productInfo.id state.authentication |> Async.RunSynchronously
            match installerInfoList.Length with
            | 1 ->
                let installerInfo = installerInfoList.[0]
                let result =
                    Games.downloadGame productInfo.title installerInfo state.authentication |> Async.RunSynchronously
                match result with
                | None -> state, Cmd.none
                | Some(task, filePath, tmppath, size) ->
                    let downloadInfo = createDownloadStatus productInfo.id (productInfo.title) filePath (float (size) / 1000000.0)

                    { state with
                          authentication = state.authentication
                          downloads = downloadInfo :: state.downloads },
                    Cmd.batch
                        [ Subs.registerDownloadTimer task tmppath downloadInfo
                          match task with
                          | Some task ->
                              let invoke() =
                                  async {
                                      let! _ = Async.AwaitTask task
                                      File.Move(tmppath, filePath) }
                              Cmd.OfAsync.perform invoke () (fun _ -> UnpackGame(state.settings.Value, downloadInfo))
                          | None -> Cmd.ofMsg <| UnpackGame(state.settings.Value, downloadInfo) ]
            | _ -> state, Cmd.ofMsg <| AddNotification "Found multiple installers, this is not supported yet..."
        | UnpackGame(settings, downloadInfo) ->
            let invoke() = Games.extractLibrary settings downloadInfo.gameTitle downloadInfo.filePath
            state,
            Cmd.batch
                [ Cmd.ofMsg <| UpdateDownloadInstalling downloadInfo.filePath
                  Cmd.OfAsync.perform invoke () (fun _ -> FinishGameDownload(downloadInfo.filePath, settings)) ]
        | UpgradeGames ->
            let (updateDataList, authentication) =
                Installed.checkAllForUpdates state.installedGames state.authentication

            let cmdList =
                match updateDataList with
                | updateDataList when updateDataList.Length > 0 ->
                    List.map (fun (updateData: Installed.UpdateData) ->
                        updateData.game
                        |> gameToDownloadInfo
                        |> StartGameDownload
                        |> Cmd.ofMsg) updateDataList
                | _ -> [ Cmd.ofMsg <| AddNotification "No games found to update." ]
            { state with authentication = authentication }, Cmd.batch cmdList
        | UpdateDownloadSize(filePath, fileSize) ->
            let downloads =
                state.downloads
                |> List.map
                    (fun download ->
                        if download.filePath = filePath then { download with downloaded = fileSize } else download)
            { state with downloads = downloads }, Cmd.none
        | UpdateDownloadInstalling filePath ->
            let downloads =
                state.downloads
                |> List.map
                    (fun download ->
                        if download.filePath = filePath then { download with installing = true } else download)
            { state with downloads = downloads }, Cmd.none

    let private leftBarView dispatch =
        LeftBar.view (fun game _ -> StartGame game |> dispatch) (fun _ -> OpenSettingsWindow false |> dispatch)

    let private notificationItemView (notification: string) =
        StackPanel.create
            [ StackPanel.classes [ "info" ]
              StackPanel.children
                  [ Grid.create
                      [ Grid.columnDefinitions "1*, Auto"
                        Grid.margin 6.0
                        Grid.children
                            [ TextBlock.create
                                [ Grid.column 0
                                  TextBlock.text notification ] ] ] ] ]

    let private notificationsView (notifications: string list) =
        match notifications with
        | notifications when notifications.Length > 0 ->
            StackPanel.create
                [ StackPanel.dock Dock.Top
                  StackPanel.classes [ "dark" ]
                  StackPanel.children
                      [ ItemsControl.create
                          [ ItemsControl.dataItems notifications
                            ItemsControl.itemTemplate (DataTemplateView<string>.create notificationItemView) ] ] ]
        | _ -> StackPanel.create [ StackPanel.dock Dock.Top ]

    let private mainAreaView (state: State) (dispatch: Msg -> unit) =
        DockPanel.create
            [ Grid.column 1
              DockPanel.horizontalAlignment HorizontalAlignment.Stretch
              DockPanel.verticalAlignment VerticalAlignment.Stretch
              DockPanel.children
                  [ notificationsView state.notifications
                    StackPanel.create
                        [ StackPanel.dock Dock.Top
                          StackPanel.margin 10.0
                          StackPanel.orientation Orientation.Horizontal
                          StackPanel.spacing 5.0
                          StackPanel.children
                              [ Button.create
                                  [ Button.content "Install game"
                                    Button.onClick (fun _ -> OpenInstallGameWindow |> dispatch) ]
                                Button.create
                                    [ Button.content "Upgrade games"
                                      Button.onClick (fun _ -> UpgradeGames |> dispatch) ] ] ]
                    TextBox.create
                        [ TextBox.dock Dock.Bottom
                          TextBox.height 100.0
                          TextBox.isReadOnly true
                          TextBox.text state.terminalOutput ]
                    StackPanel.create
                        [ StackPanel.orientation Orientation.Vertical
                          StackPanel.margin 10.0
                          StackPanel.children
                              [ TextBlock.create
                                  [ TextBlock.textWrapping TextWrapping.Wrap
                                    TextBlock.text "Because I couldn't make a browser control for Avalonia work, we have to live for now \
                                without the graphical stuff from GOG Galaxy and use some workaround buttons (and later menus).\n\
                                You can start games by right clicking on the game in the list on the left side.\n\
                                \n\
                                If a game does not start on linux, run Andromeda from console and have a look at the console output. \
                                This happens normally, because the execution rights are not set correctly for some files.\n\
                                \n\
                                On windows many games crash after start for some reason. You have to start it manually \
                                from outside Andromeda for now.\n\
                                Upgrading will not work on windows as well. This will hopefully be fixed for v0.4.0.\n\
                                \n\
                                Working on a solution for those problems!" ] ] ] ] ]

    let view (state: State) (dispatch: Msg -> unit) =
        DockPanel.create
            [ DockPanel.verticalAlignment VerticalAlignment.Stretch
              DockPanel.horizontalAlignment HorizontalAlignment.Stretch
              DockPanel.lastChildFill true
              DockPanel.children
                  [ Grid.create
                      [ Grid.columnDefinitions "1*, 3*"
                        Grid.children
                            [ leftBarView dispatch state.installedGames state.downloads state.leftBarState
                                  (LeftBarMsg >> dispatch)
                              mainAreaView state dispatch ] ] ] ]

    type MainWindow() as this =
        inherit HostWindow()
        do
            base.Title <- "Andromeda"
            base.Icon <- WindowIcon (AvaloniaHelper.loadAssetPath "avares://Andromeda.AvaloniaApp.FSharp/Assets/logo.ico")
            base.Width <- 1024.0
            base.Height <- 660.0

#if DEBUG
            this.AttachDevTools(KeyGesture(Key.F12))
#endif

            let settings = SettingsPersistence.load()

            let authentication =
                match AuthenticationPersistence.load() with
                | Some appData -> appData
                | None -> Authentication.NoAuth

            let syncDispatch (dispatch: Dispatch<'msg>): Dispatch<'msg> =
                match Dispatcher.UIThread.CheckAccess() with
                | true -> fun msg -> Dispatcher.UIThread.Post(fun () -> dispatch msg)
                | false -> dispatch

            Program.mkProgram init update view
            |> Program.withHost this
            |> Program.withSyncDispatch syncDispatch
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWith (settings, authentication, this)
