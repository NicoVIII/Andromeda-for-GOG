namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Andromeda.Core.Lenses
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
open GogApi.DotNet.FSharp
open GogApi.DotNet.FSharp.DomainTypes
open System
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
          authenticationState: Authentication.State
          leftBarState: LeftBar.State
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

    type Msg<'T> =
        | GlobalMsg of Global.Msg<'T>
        | AuthenticationMsg of Authentication.Msg<'T>
        | LeftBarMsg of LeftBar.Msg
        | AddNotification of string
        | RemoveNotification of string
        | AddToTerminalOutput of string
        | OpenInstallGameWindow of Authentication
        | CloseAllWindows
        | CloseSettingsWindow of Settings.IWindow * Settings * Authentication
        | CloseInstallGameWindow of ProductInfo * Authentication
        | SearchInstalled of Authentication
        | SetSettings of Settings * Authentication
        | StartGameDownload of ProductInfo * Authentication
        | UnpackGame of Settings * DownloadStatus * version: string option * Authentication
        | FinishGameDownload of string * Authentication
        | UpdateDownloadSize of ProductId * int
        | UpdateDownloadInstalling of string
        | UpgradeGames of Authentication

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

                Installed.startGameProcess showGameOutput game.path
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
                    wind.OnSave.Subscribe(fun (_, downloadInfo) ->
                        (downloadInfo, authentication)
                        |> CloseInstallGameWindow
                        |> dispatch)
                    |> ignore
                | None -> ()

            Cmd.ofSub sub

        let saveSettings authentication (window: Settings.IWindow) =
            let sub dispatch =
                window.OnSave.Subscribe(fun (window, settings) ->
                    (window, settings, authentication)
                    |> CloseSettingsWindow
                    |> dispatch)
                |> ignore

            Cmd.ofSub sub

    let init (settings: Settings option, authentication: Authentication option) =
        let authentication =
            Option.bind
                (Authentication.getRefreshToken
                 >> Async.RunSynchronously) authentication

        Option.map Persistence.Authentication.save |> ignore

        let state =
            { globalState = Global.init authentication settings
              authenticationState = Authentication.init ()
              leftBarState = LeftBar.init ()
              installGameWindow = None
              notifications = []
              settingsWindow = None
              terminalOutput = "" }

        state, Cmd.none

    let updateGlobal msg (state: State) (mainWindow: AndromedaWindow) =
        match msg with
        | Global.UseLens (lens, value) ->
            let state =
                state.globalState
                |> setl lens value
                |> setl StateLenses.globalState
                <| state

            state, Cmd.none
        | Global.Authenticate authentication ->
            Persistence.Authentication.save authentication
            |> ignore

            let state =
                setl StateLenses.authentication (Some authentication) state

            state, Cmd.none
        | Global.ChangeMode mode -> setl StateLenses.mode mode state, Cmd.none
        | Global.OpenSettingsWindow authentication ->
            let window =
                Settings.SettingsWindow(getl StateLenses.settings state)

            window.ShowDialog(mainWindow) |> ignore

            let cmd = Subs.saveSettings authentication window

            { state with
                  settingsWindow = window |> Some },
            cmd
        | Global.StartGame installedGame -> state, Subs.startGame installedGame

    let update msg (state: State) (mainWindow: AndromedaWindow) =
        match msg with
        | GlobalMsg msg -> updateGlobal msg state mainWindow
        | AuthenticationMsg msg ->
            let (s, cmd) =
                Authentication.update msg state.authenticationState AuthenticationMsg
                    GlobalMsg

            { state with authenticationState = s }, cmd
        | CloseAllWindows ->
            let closeWindow (window: IAndromedaWindow) =
                window.CloseWithoutCustomHandler()

            Option.iter closeWindow state.settingsWindow
            closeWindow mainWindow

            state, Cmd.none
        | LeftBarMsg msg ->
            let (s, cmd) = LeftBar.update msg state.leftBarState

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
        | OpenInstallGameWindow authentication ->
            let window =
                InstallGame.InstallGameWindow
                    (authentication,
                     getl StateLenses.installedGames state
                     |> List.map (fun game -> game.id))

            window.ShowDialog(mainWindow) |> ignore

            let state =
                { state with
                      installGameWindow = window |> Some }

            state, Subs.installGameWindow authentication state
        | CloseInstallGameWindow (downloadInfo, authentication) ->
            let cmd =
                (downloadInfo, authentication)
                |> StartGameDownload
                |> Cmd.ofMsg

            { state with installGameWindow = None }, cmd
        | CloseSettingsWindow (window, settings, authentication) ->
            window.CloseWithoutCustomHandler()

            // After we got new settings, we perform a cache check
            Cache.check settings

            let cmd =
                (settings, authentication)
                |> SetSettings
                |> Cmd.ofMsg

            { state with settingsWindow = None }, cmd
        | SearchInstalled authentication ->
            let settings = getl StateLenses.settings state

            let installedGames =
                Installed.searchInstalled settings authentication

            let state =
                setl StateLenses.installedGames installedGames state

            (state, Cmd.none)
        | SetSettings (settings, authentication) ->
            Persistence.Settings.save settings |> ignore

            let state = setl StateLenses.settings settings state

            let msg =
                Cmd.ofMsg (authentication |> SearchInstalled)

            (state, msg)
        | FinishGameDownload (filePath, authentication) ->
            let statusList =
                getl StateLenses.downloads state
                |> List.filter (fun ds -> ds.filePath <> filePath)

            setl StateLenses.downloads statusList state,
            Cmd.ofMsg (authentication |> SearchInstalled)
        | StartGameDownload (productInfo, authentication) ->
            let installerInfoList =
                Diverse.getAvailableInstallersForOs productInfo.id authentication
                |> Async.RunSynchronously

            match installerInfoList.Length with
            | 1 ->
                let installerInfo = installerInfoList.[0]

                let result =
                    Download.downloadGame productInfo.title installerInfo authentication
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
                                      (getl StateLenses.settings state,
                                       downloadInfo,
                                       installerInfo.version,
                                       authentication))
                          | None ->
                              Cmd.ofMsg
                              <| UnpackGame
                                  (getl StateLenses.settings state,
                                   downloadInfo,
                                   installerInfo.version,
                                   authentication) ]
            | 0 -> state, Cmd.ofMsg (AddNotification "Found no installer for this OS...")
            | _ ->
                state,
                Cmd.ofMsg
                    (AddNotification
                        "Found multiple installers, this is not supported yet...")
        | UnpackGame (settings, downloadInfo, version, authentication) ->
            let invoke () =
                Download.extractLibrary settings downloadInfo.gameTitle downloadInfo.filePath
                    version

            let cmd =
                [ UpdateDownloadInstalling downloadInfo.filePath
                  |> Cmd.ofMsg

                  (fun _ -> FinishGameDownload(downloadInfo.filePath, authentication))
                  |> Cmd.OfAsync.perform invoke () ]
                |> Cmd.batch

            state, cmd
        | UpgradeGames authentication ->
            let (updateDataList, authentication) =
                (getl StateLenses.installedGames state, authentication)
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
                        |> (fun productInfo ->
                            (productInfo, authentication) |> StartGameDownload)
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

    let private mainAreaView authentication (state: State) dispatch gDispatch =
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
                                        authentication
                                        |> OpenInstallGameWindow
                                        |> dispatch) ]
                                Button.create
                                    [ Button.content "Upgrade games"
                                      Button.onClick (fun _ ->
                                          authentication |> UpgradeGames |> dispatch) ] ] ]
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
                                   GameList.view gDispatch
                                       (getl StateLenses.installedGames state)
                                       authentication) ] ] ]

    let leftBarView authentication state dispatch =
        LeftBar.view authentication state.leftBarState state.globalState
            (LeftBarMsg >> dispatch) (GlobalMsg >> dispatch)

    let view (state: State) dispatch =
        let gDispatch = (GlobalMsg >> dispatch)
        match getl StateLenses.authentication state with
        | Some authentication ->
            DockPanel.create
                [ DockPanel.verticalAlignment VerticalAlignment.Stretch
                  DockPanel.horizontalAlignment HorizontalAlignment.Stretch
                  DockPanel.lastChildFill true
                  DockPanel.children
                      [ Grid.create
                          [ Grid.columnDefinitions "1*, 3*"
                            Grid.children
                                [ leftBarView authentication state dispatch
                                  mainAreaView authentication state dispatch gDispatch ] ] ] ] :> IView
        | None ->
            Authentication.view state.authenticationState (AuthenticationMsg >> dispatch)

    type MainWindow() as this =
        inherit AndromedaWindow()

        do
            base.Title <- "Andromeda"
            base.Icon <-
                WindowIcon
                    (AvaloniaHelper.loadAssetPath
                        "avares://Andromeda.AvaloniaApp/Assets/logo.ico")
            base.Width <- 1024.0
            base.Height <- 660.0

#if DEBUG
            this.AttachDevTools(KeyGesture(Key.F12))
#endif

            // Load saved stuff from dsik
            let settings = Persistence.Settings.load ()
            let authentication = Persistence.Authentication.load ()

            let updateWithServices msg state = update msg state this

            Program.mkProgram init updateWithServices view
            |> Program.withHost this
            |> Program.withSubscription (fun _ -> Subs.closeWindow this)
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWith (settings, authentication)
