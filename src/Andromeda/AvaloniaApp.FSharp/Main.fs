namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.Lenses
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Threading
open Elmish
open GogApi.DotNet.FSharp.DomainTypes
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
        { globalState: Global.State
          leftBarState: LeftBar.State
          authenticationWindow: Authentication.Window option
          installGameWindow: InstallGame.InstallGameWindow option
          notifications: string list
          settingsWindow: Settings.SettingsWindow option
          terminalOutput: string }

    module StateLenses =
        // Lenses
        let globalState =
            Lens((fun s -> s.globalState), (fun s g -> { s with globalState = g }))

        let authentication =
            globalState << Global.StateLenses.authentication

        let downloads =
            globalState << Global.StateLenses.downloads

        let installedGames =
            globalState << Global.StateLenses.installedGames

        let mode = globalState << Global.StateLenses.mode

        let settings =
            globalState << Global.StateLenses.settings

    type Msg =
        | GlobalMessage of Global.Message
        | LeftBarMsg of LeftBar.Msg
        | AddNotification of string
        | RemoveNotification of string
        | AddToTerminalOutput of string
        | OpenAuthenticationWindow
        | OpenInstallGameWindow
        | CloseAllWindows
        | CloseAuthenticationWindow of Authentication.IWindow * Authentication
        | CloseSettingsWindow of Settings.IWindow * Settings
        | CloseInstallGameWindow of ProductInfo
        | SearchInstalled of Settings
        | SetSettings of Settings
        | StartGameDownload of ProductInfo
        | UnpackGame of Tuple<Settings, DownloadStatus, string option>
        | FinishGameDownload of Tuple<string, Settings>
        | UpdateDownloadSize of Tuple<ProductId, int>
        | UpdateDownloadInstalling of string
        | UpgradeGames

    module Subs =
        let startGame (game: InstalledGame) =
            let sub dispatch =
                let showGameOutput _ (outLine: DataReceivedEventArgs) =
                    if outLine.Data |> String.IsNullOrEmpty |> not then
                        AvaloniaScheduler.Instance.Schedule
                            (outLine.Data,
                             (fun _ newLine ->
                                 AddToTerminalOutput newLine |> dispatch
                                 { new IDisposable with
                                     member __.Dispose() = () }))
                        |> ignore
                    else
                        ()

                Games.startGameProcess showGameOutput game.path
                |> ignore

            Cmd.ofSub sub

        let registerDownloadTimer
            (task: Task option)
            tmppath
            (downloadInfo: DownloadStatus)
            =
            let sub dispatch =
                match task with
                | Some _ ->
                    "Download installer for "
                    + downloadInfo.gameTitle
                    + "."
                    |> Logger.LogInfo

                    let invoke () =
                        let fileSize =
                            if File.Exists tmppath then
                                let fileSize =
                                    tmppath
                                    |> FileInfo
                                    |> fun fileInfo ->
                                        int (float (fileInfo.Length) / 1000000.0)

                                UpdateDownloadSize(downloadInfo.gameId, fileSize)
                                |> dispatch
                                fileSize
                            else
                                0

                        File.Exists tmppath
                        && fileSize < int downloadInfo.fileSize

                    DispatcherTimer.Run(Func<bool>(invoke), TimeSpan.FromSeconds 0.5)
                    |> ignore
                | None ->
                    "Use cached installer for "
                    + downloadInfo.gameTitle
                    + "."
                    |> Logger.LogInfo
                    UpdateDownloadSize(downloadInfo.gameId, int downloadInfo.fileSize)
                    |> dispatch

            Cmd.ofSub sub

        let removeNotification notification =
            let sub dispatch =
                let invoke () =
                    RemoveNotification notification |> dispatch

                DispatcherTimer.RunOnce(Action(invoke), TimeSpan.FromSeconds 5.0)
                |> ignore

            Cmd.ofSub sub

        let saveAuthentication (wind: Authentication.IWindow) =
            let sub dispatch =
                wind.OnSave.Subscribe(CloseAuthenticationWindow >> dispatch)
                |> ignore

            Cmd.ofSub sub

        let closeWindow (wind: IAndromedaWindow) =
            let sub (dispatch: Msg -> unit) =
                wind.AddClosedHandler (fun _ -> CloseAllWindows |> dispatch)
                |> ignore

            Cmd.ofSub sub

        let installGameWindow state =
            let sub dispatch =
                match state.installGameWindow with
                | Some window ->
                    let wind = window :> InstallGame.IInstallGameWindow
                    wind.OnSave.Subscribe(fun (_, downloadInfo) ->
                        downloadInfo |> CloseInstallGameWindow |> dispatch)
                    |> ignore
                | None -> ()

            Cmd.ofSub sub

        let saveSettings (window: Settings.IWindow) =
            let sub dispatch =
                window.OnSave.Subscribe(CloseSettingsWindow >> dispatch)
                |> ignore

            Cmd.ofSub sub

    let init (settings: Settings option, authentication: Authentication option) =
        let authentication =
            Option.bind
                (Authentication.getRefreshToken
                 >> Async.RunSynchronously) authentication

        Option.map AuthenticationPersistence.save
        |> ignore

        let state =
            { globalState = Global.init authentication settings
              leftBarState = LeftBar.init ()
              authenticationWindow = None
              installGameWindow = None
              notifications = []
              settingsWindow = None
              terminalOutput = "" }

        let authenticationCommand =
            match authentication with
            | Some _ -> Cmd.none
            | None -> Cmd.ofMsg OpenAuthenticationWindow

        let settingsCommand =
            match settings with
            | Some settings -> SetSettings settings |> Cmd.ofMsg
            | None ->
                Global.OpenSettingsWindow true
                |> GlobalMessage
                |> Cmd.ofMsg

        state,
        Cmd.batch
            [ authenticationCommand
              settingsCommand ]

    let updateGlobal (msg: Global.Message) (state: State) (mainWindow: AndromedaWindow) =
        match msg with
        | Global.ChangeMode mode -> setl StateLenses.mode mode state, Cmd.none
        | Global.OpenSettingsWindow initial ->
            let window =
                Settings.SettingsWindow(getl StateLenses.settings state, initial)

            window.ShowDialog(mainWindow) |> ignore

            let cmd =
                Cmd.batch
                    [ if initial then Subs.closeWindow window else ()
                      Subs.saveSettings window ]

            { state with
                  settingsWindow = window |> Some },
            cmd
        | Global.StartGame installedGame -> state, Subs.startGame installedGame

    let update (msg: Msg) (state: State) (mainWindow: AndromedaWindow) =
        match msg with
        | GlobalMessage msg -> updateGlobal msg state mainWindow
        | CloseAllWindows ->
            let closeWindow (window: IAndromedaWindow) =
                window.CloseWithoutCustomHandler()

            Option.iter closeWindow state.authenticationWindow
            Option.iter closeWindow state.settingsWindow
            closeWindow mainWindow

            let state =
                { state with
                      authenticationWindow = None
                      settingsWindow = None }

            state, Cmd.none
        | LeftBarMsg leftBarMsg ->
            let (s, cmd) =
                LeftBar.update leftBarMsg state.leftBarState

            { state with leftBarState = s }, Cmd.map LeftBarMsg cmd
        | AddNotification notification ->
            { state with
                  notifications = notification :: state.notifications },
            Subs.removeNotification notification
        | RemoveNotification notification ->
            let notifications =
                state.notifications
                |> List.filter (fun n -> n <> notification)

            { state with
                  notifications = notifications },
            Cmd.none
        | AddToTerminalOutput newLine ->
            let terminalOutput =
                match state.terminalOutput with
                | "" -> newLine
                | _ ->
                    newLine
                    + Environment.NewLine
                    + state.terminalOutput

            { state with
                  terminalOutput = terminalOutput },
            Cmd.none
        | OpenAuthenticationWindow ->
            let window = Authentication.Window()

            window.ShowDialog(mainWindow) |> ignore

            let state =
                { state with
                      authenticationWindow = window |> Some }

            let cmd =
                Cmd.batch
                    [ Subs.saveAuthentication window
                      Subs.closeWindow window ]

            state, cmd
        | OpenInstallGameWindow ->
            let window =
                InstallGame.InstallGameWindow
                    ((getl StateLenses.authentication state).Value,
                     getl StateLenses.installedGames state
                     |> List.map (fun game -> game.id))

            window.ShowDialog(mainWindow) |> ignore

            let state =
                { state with
                      installGameWindow = window |> Some }

            state, Subs.installGameWindow state
        | CloseAuthenticationWindow (window, authentication) ->
            AuthenticationPersistence.save authentication
            |> ignore

            window.CloseWithoutCustomHandler()

            let state =
                { state with
                      authenticationWindow = None }
                |> setl StateLenses.authentication (Some authentication)

            state, Cmd.none
        | CloseInstallGameWindow downloadInfo ->
            { state with installGameWindow = None },
            Cmd.ofMsg <| StartGameDownload downloadInfo
        | CloseSettingsWindow (window, settings) ->
            window.CloseWithoutCustomHandler()

            { state with settingsWindow = None }, Cmd.ofMsg (SetSettings settings)
        | SearchInstalled settings ->
            let installedGames =
                Installed.searchInstalled settings
                    (getl StateLenses.authentication state).Value

            let state =
                setl StateLenses.installedGames installedGames state

            (state, Cmd.none)
        | SetSettings settings ->
            SettingsPersistence.save settings |> ignore

            let state =
                setl StateLenses.settings (Some settings) state

            let msg = Cmd.ofMsg (settings |> SearchInstalled)

            (state, msg)
        | FinishGameDownload (filePath, settings) ->
            let statusList =
                getl StateLenses.downloads state
                |> List.filter (fun ds -> ds.filePath <> filePath)

            setl StateLenses.downloads statusList state,
            Cmd.ofMsg (settings |> SearchInstalled)
        | StartGameDownload productInfo ->
            let installerInfoList =
                Games.getAvailableInstallersForOs productInfo.id
                    (getl StateLenses.authentication state).Value
                |> Async.RunSynchronously

            match installerInfoList.Length with
            | 1 ->
                let installerInfo = installerInfoList.[0]

                let result =
                    Games.downloadGame productInfo.title installerInfo
                        (getl StateLenses.authentication state).Value
                    |> Async.RunSynchronously

                match result with
                | None -> state, Cmd.none
                | Some (task, filePath, tmppath, size) ->
                    let downloadInfo =
                        createDownloadStatus productInfo.id (productInfo.title) filePath
                            (float (size) / 1000000.0)

                    (downloadInfo :: (getl StateLenses.downloads state), state)
                    ||> setl StateLenses.downloads,
                    Cmd.batch
                        [ Subs.registerDownloadTimer task tmppath downloadInfo
                          match task with
                          | Some task ->
                              let invoke () =
                                  async {
                                      let! _ = Async.AwaitTask task
                                      File.Move(tmppath, filePath)
                                  }

                              Cmd.OfAsync.perform invoke () (fun _ ->
                                  UnpackGame
                                      ((getl StateLenses.settings state).Value,
                                       downloadInfo,
                                       installerInfo.version))
                          | None ->
                              Cmd.ofMsg
                              <| UnpackGame
                                  ((getl StateLenses.settings state).Value,
                                   downloadInfo,
                                   installerInfo.version) ]
            | 0 -> state, Cmd.ofMsg (AddNotification "Found no installer for this OS...")
            | _ ->
                state,
                Cmd.ofMsg
                    (AddNotification
                        "Found multiple installers, this is not supported yet...")
        | UnpackGame (settings, downloadInfo, version) ->
            let invoke () =
                Games.extractLibrary settings downloadInfo.gameTitle downloadInfo.filePath
                    version

            let cmd =
                [ UpdateDownloadInstalling downloadInfo.filePath
                  |> Cmd.ofMsg

                  (fun _ -> FinishGameDownload(downloadInfo.filePath, settings))
                  |> Cmd.OfAsync.perform invoke () ]
                |> Cmd.batch

            state, cmd
        | UpgradeGames ->
            let (updateDataList, authentication) =
                (getl StateLenses.installedGames state,
                 (getl StateLenses.authentication state).Value)
                ||> Installed.checkAllForUpdates

            // Update authentication, if it was refreshed
            let state =
                setl StateLenses.authentication (Some authentication) state

            // Download updated installers or show notification
            let cmd =
                match updateDataList with
                | updateDataList when updateDataList.Length > 0 ->
                    List.map (fun (updateData: Installed.UpdateData) ->
                        updateData.game
                        |> gameToDownloadInfo
                        |> StartGameDownload
                        |> Cmd.ofMsg) updateDataList
                | _ ->
                    [ AddNotification "No games found to update."
                      |> Cmd.ofMsg ]
                |> Cmd.batch

            state, cmd
        | UpdateDownloadSize (gameId, fileSize) ->
            let state =
                getl StateLenses.downloads state
                |> List.map (fun download ->
                    if download.gameId = gameId then
                        { download with downloaded = fileSize }
                    else
                        download)
                |> setl StateLenses.downloads
                <| state

            state, Cmd.none
        | UpdateDownloadInstalling filePath ->
            let state =
                getl StateLenses.downloads state
                |> List.map (fun download ->
                    if download.filePath = filePath then
                        { download with installing = true }
                    else
                        download)
                |> setl StateLenses.downloads
                <| state

            state, Cmd.none

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
                            ItemsControl.itemTemplate
                                (DataTemplateView<string>.create notificationItemView) ] ] ]
        | _ -> StackPanel.create [ StackPanel.dock Dock.Top ]

    let private mainAreaView (state: State) dispatch gDispatch =
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
                                    Button.onClick (fun _ ->
                                        OpenInstallGameWindow |> dispatch) ]
                                Button.create
                                    [ Button.content "Upgrade games"
                                      Button.onClick (fun _ -> UpgradeGames |> dispatch) ] ] ]
                    TextBox.create
                        [ TextBox.dock Dock.Bottom
                          TextBox.height 100.0
                          TextBox.isReadOnly true
                          TextBox.text state.terminalOutput ]
                    ScrollViewer.create
                        [ ScrollViewer.horizontalScrollBarVisibility
                            ScrollBarVisibility.Disabled
                          ScrollViewer.padding 10.0
                          ScrollViewer.content
                              (if getl StateLenses.mode state = Global.Empty then
                                  TextBlock.create
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
                                    Working on a solution for those problems!" ] :> IView
                               else
                                   match getl StateLenses.authentication state with
                                   | Some authentication ->
                                       Games.view gDispatch
                                           (getl StateLenses.installedGames state)
                                           authentication
                                   | None ->
                                       (AvaloniaHelper.simpleTextBlock "Please log in.") :> IView) ] ] ]

    let leftBarView state dispatch =
        LeftBar.view state.leftBarState state.globalState (LeftBarMsg >> dispatch)
            (GlobalMessage >> dispatch)

    let view (state: State) dispatch =
        let gDispatch = (GlobalMessage >> dispatch)
        DockPanel.create
            [ DockPanel.verticalAlignment VerticalAlignment.Stretch
              DockPanel.horizontalAlignment HorizontalAlignment.Stretch
              DockPanel.lastChildFill true
              DockPanel.children
                  [ Grid.create
                      [ Grid.columnDefinitions "1*, 3*"
                        Grid.children
                            [ leftBarView state dispatch
                              mainAreaView state dispatch gDispatch ] ] ] ]

    type MainWindow() as this =
        inherit AndromedaWindow()

        do
            base.Title <- "Andromeda"
            base.Icon <-
                WindowIcon
                    (AvaloniaHelper.loadAssetPath
                        "avares://Andromeda.AvaloniaApp.FSharp/Assets/logo.ico")
            base.Width <- 1024.0
            base.Height <- 660.0

#if DEBUG
            this.AttachDevTools(KeyGesture(Key.F12))
#endif

            let settings = SettingsPersistence.load ()

            let authentication =
                match AuthenticationPersistence.load () with
                | Some authentication -> Some authentication
                | None -> None

            let updateWithServices msg state = update msg state this

            Program.mkProgram init updateWithServices view
            |> Program.withHost this
            |> Program.withSubscription (fun _ -> Subs.closeWindow this)
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWith (settings, authentication)
