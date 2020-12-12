namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Andromeda.Core.Lenses
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Threading
open Elmish
open GogApi.DotNet.FSharp.DomainTypes
open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks

open Andromeda.AvaloniaApp.AvaloniaHelper

module Main =
    type State =
        { authentication: Authentication
          downloads: DownloadStatus list
          installedGames: Map<ProductId, InstalledGame>
          mode: Mode
          notifications: string list
          settings: Settings
          terminalOutput: string }

    module StateLenses =
        // Lenses
        let authentication =
            Lens
                ((fun r -> r.authentication),
                 (fun r v -> { r with authentication = v }))

        let downloads =
            Lens((fun r -> r.downloads), (fun r v -> { r with downloads = v }))

        let installedGames =
            Lens
                ((fun r -> r.installedGames),
                 (fun r v -> { r with installedGames = v }))

        let mode =
            Lens((fun r -> r.mode), (fun r v -> { r with mode = v }))

        let notifications =
            Lens
                ((fun r -> r.notifications),
                 (fun r v -> { r with notifications = v }))

        let settings =
            Lens((fun r -> r.settings), (fun r v -> { r with settings = v }))

        let terminalOutput =
            Lens
                ((fun r -> r.terminalOutput),
                 (fun r v -> { r with terminalOutput = v }))

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
        | SetSettings of Settings * Authentication
        | SearchInstalled of Authentication
        | StartGameDownload of ProductInfo * Authentication
        | UnpackGame of
            Settings *
            DownloadStatus *
            version: string option *
            Authentication
        | FinishGameDownload of string * Authentication
        | UpdateDownloadSize of ProductId * int
        | UpdateDownloadInstalling of string
        | UpgradeGames
        // Intent messages
        | OpenSettings
        | OpenInstallGameWindow

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

        let registerDownloadTimer (task: Task option)
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
                                        int
                                            (float (fileInfo.Length) / 1000000.0)

                                UpdateDownloadSize
                                    (downloadInfo.gameId, fileSize)
                                |> dispatch

                                fileSize
                            else
                                0

                        File.Exists tmppath
                        && fileSize < int downloadInfo.fileSize

                    DispatcherTimer.Run
                        (Func<bool>(invoke), TimeSpan.FromSeconds 0.5)
                    |> ignore
                | None ->
                    "Use cached installer for "
                    + downloadInfo.gameTitle
                    + "."
                    |> Logger.LogInfo

                    UpdateDownloadSize
                        (downloadInfo.gameId, int downloadInfo.fileSize)
                    |> dispatch

            Cmd.ofSub sub

    let init (settings: Settings option) (authentication: Authentication) =
        let settings =
            settings
            |> Option.defaultValue (SystemInfo.defaultSettings ())

        // After determining our settings, we perform a cache check
        Cache.check settings

        let (installedGames, imgJobs) =
            Installed.searchInstalled settings authentication

        let state =
            { authentication = authentication
              downloads = []
              installedGames = installedGames
              mode = Installed
              notifications = []
              settings = settings
              terminalOutput = "" }

        let cmd =
            imgJobs
            |> List.map
                (fun job -> Cmd.OfAsync.perform job authentication SetGameImage)
            |> Cmd.batch

        state, cmd

    let update msg (state: State) =
        match msg with
        | ChangeState change ->
            let state = change state

            state, Cmd.none, DoNothing
        | ChangeMode mode ->
            setl StateLenses.mode mode state, Cmd.none, DoNothing
        | StartGame installedGame ->
            state, Subs.startGame installedGame, DoNothing
        | UpgradeGame game ->
            let (updateData, authentication) =
                (getl StateLenses.authentication state, game)
                ||> Installed.checkGameForUpdate

            // Update authentication in state, if it was refreshed
            let state =
                setl StateLenses.authentication authentication state

            // Download updated installers or show notification
            let cmd =
                match updateData with
                | Some updateData ->
                    updateData.game
                    |> gameToDownloadInfo
                    |> (fun productInfo ->
                        (productInfo, authentication) |> StartGameDownload)
                    |> Cmd.ofMsg
                | None ->
                    AddNotification
                        $"No new version available for %s{game.name}"
                    |> Cmd.ofMsg

            state, cmd, DoNothing
        | SetGameImage (productId, imgPath) ->
            let state =
                state
                |> getl StateLenses.installedGames
                |> Map.change
                    productId
                    (function
                    | Some game -> { game with image = imgPath |> Some } |> Some
                    | None -> None)
                |> setlr StateLenses.installedGames state

            state, Cmd.none, DoNothing
        | AddNotification notification ->
            // Remove notification again after 5 seconds
            let removeNotification notification =
                async {
                    do! Async.Sleep 5000
                    return notification
                }

            let state =
                state
                |> getl StateLenses.notifications
                |> List.append [ notification ]
                |> setlr StateLenses.notifications state

            let cmd =
                Cmd.OfAsync.perform
                    removeNotification
                    notification
                    RemoveNotification

            state, cmd, DoNothing
        | RemoveNotification notification ->
            let notifications =
                state.notifications
                |> List.filter (fun n -> n <> notification)

            { state with
                  notifications = notifications },
            Cmd.none,
            DoNothing
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
            Cmd.none,
            DoNothing
        | SearchInstalled authentication ->
            let settings = getl StateLenses.settings state

            let (installedGames, imgJobs) =
                Installed.searchInstalled settings authentication

            let state =
                setl StateLenses.installedGames installedGames state

            let cmd =
                imgJobs
                |> List.map
                    (fun job ->
                        Cmd.OfAsync.perform job authentication SetGameImage)
                |> Cmd.batch

            state, cmd, DoNothing
        | SetSettings (settings, authentication) ->
            Persistence.Settings.save settings |> ignore

            let state = setl StateLenses.settings settings state

            // After we got new settings, we perform a cache check
            Cache.check settings

            let msg =
                Cmd.ofMsg (authentication |> SearchInstalled)

            state, msg, DoNothing
        | FinishGameDownload (filePath, authentication) ->
            let statusList =
                getl StateLenses.downloads state
                |> List.filter (fun ds -> ds.filePath <> filePath)

            setl StateLenses.downloads statusList state,
            Cmd.ofMsg (authentication |> SearchInstalled),
            DoNothing
        | StartGameDownload (productInfo, authentication) ->
            let installerInfoList =
                Diverse.getAvailableInstallersForOs
                    productInfo.id
                    authentication
                |> Async.RunSynchronously

            match installerInfoList.Length with
            | 1 ->
                let installerInfo = installerInfoList.[0]

                let result =
                    Download.downloadGame
                        productInfo.title
                        installerInfo
                        authentication
                    |> Async.RunSynchronously

                match result with
                | None -> state, Cmd.none, DoNothing
                | Some (task, filePath, tmppath, size) ->
                    let downloadInfo =
                        DownloadStatus.create
                            productInfo.id
                            (productInfo.title)
                            filePath
                            (float (size) / 1000000.0)

                    (downloadInfo :: (getl StateLenses.downloads state), state)
                    ||> setl StateLenses.downloads,
                    Cmd.batch [
                        Subs.registerDownloadTimer task tmppath downloadInfo
                        match task with
                        | Some task ->
                            let invoke () =
                                async {
                                    let! _ = Async.AwaitTask task
                                    File.Move(tmppath, filePath)
                                }

                            Cmd.OfAsync.perform
                                invoke
                                ()
                                (fun _ ->
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
                                 authentication)
                    ],
                    DoNothing
            | 0 ->
                state,
                Cmd.ofMsg (AddNotification "Found no installer for this OS..."),
                DoNothing
            | _ ->
                state,
                Cmd.ofMsg
                    (AddNotification
                        "Found multiple installers, this is not supported yet..."),
                DoNothing
        | UnpackGame (settings, downloadInfo, version, authentication) ->
            let invoke () =
                Download.extractLibrary
                    settings
                    downloadInfo.gameTitle
                    downloadInfo.filePath
                    version

            let cmd =
                [ UpdateDownloadInstalling downloadInfo.filePath
                  |> Cmd.ofMsg

                  (fun _ ->
                      FinishGameDownload(downloadInfo.filePath, authentication))
                  |> Cmd.OfAsync.perform invoke () ]
                |> Cmd.batch

            state, cmd, Intent.DoNothing
        | UpgradeGames ->
            let (updateDataList, authentication) =
                (getl StateLenses.installedGames state,
                 getl StateLenses.authentication state)
                ||> Installed.checkAllForUpdates

            // Update authentication in state, if it was refreshed
            let state =
                setl StateLenses.authentication authentication state

            // Download updated installers or show notification
            let cmd =
                match updateDataList with
                | updateDataList when updateDataList.Length > 0 ->
                    List.map
                        (fun (updateData: Installed.UpdateData) ->
                            updateData.game
                            |> gameToDownloadInfo
                            |> (fun productInfo ->
                                (productInfo, authentication)
                                |> StartGameDownload)
                            |> Cmd.ofMsg)
                        updateDataList
                | _ ->
                    [ AddNotification "No games found to update."
                      |> Cmd.ofMsg ]
                |> Cmd.batch

            state, cmd, DoNothing
        | UpdateDownloadSize (gameId, fileSize) ->
            let state =
                getl StateLenses.downloads state
                |> List.map
                    (fun download ->
                        if download.gameId = gameId then
                            { download with downloaded = fileSize }
                        else
                            download)
                |> setlr StateLenses.downloads state

            state, Cmd.none, DoNothing
        | UpdateDownloadInstalling filePath ->
            let state =
                getl StateLenses.downloads state
                |> List.map
                    (fun download ->
                        if download.filePath = filePath then
                            { download with installing = true }
                        else
                            download)
                |> setlr StateLenses.downloads state

            state, Cmd.none, DoNothing
        // Intents
        | OpenSettings -> state, Cmd.none, Intent.OpenSettings
        | OpenInstallGameWindow -> state, Cmd.none, Intent.OpenInstallGameWindow

    let gameTile dispatch (i: int, game: InstalledGame): IView =
        let gap = 5.0

        Border.create [
            Border.margin (0.0, 0.0, gap, gap)
            Border.contextMenu
                (ContextMenu.create [
                    ContextMenu.viewItems [
                        MenuItem.create [
                            MenuItem.header "Start"
                            MenuItem.onClick
                                ((fun _ -> game |> StartGame |> dispatch),
                                 OnChangeOf game)
                        ]
                        MenuItem.create [
                            MenuItem.header "Update"
                            MenuItem.onClick
                                ((fun _ -> game |> UpgradeGame |> dispatch),
                                 OnChangeOf game)
                        ]
                        MenuItem.create [ MenuItem.header "-" ]
                        MenuItem.create [
                            MenuItem.header "Open game folder"
                            MenuItem.onClick
                                ((fun _ -> game.path |> System.openFolder),
                                 OnChangeOf game)
                        ]
                    ]
                 ])
            Border.child
                (Image.create [
                    Image.height 120.0
                    Image.stretch Stretch.UniformToFill
                    Image.width 200.0
                    Image.source
                        (match game.image with
                         | Some imgPath -> new Bitmap(imgPath)
                         | None ->
                             new Bitmap(loadAssetPath
                                            "avares://Andromeda.AvaloniaApp/Assets/placeholder.jpg"))
                 ])
        ]
        :> IView

    let renderGameList state dispatch =
        WrapPanel.create [
            WrapPanel.children
                (state
                 |> getl StateLenses.installedGames
                 |> Map.toList
                 |> List.map snd
                 |> List.indexed
                 |> List.map (gameTile dispatch))
        ]

    let private notificationItemView (notification: string) =
        StackPanel.create [
            StackPanel.classes [ "info" ]
            StackPanel.children [
                Grid.create [
                    Grid.columnDefinitions "1*, Auto"
                    Grid.margin 6.0
                    Grid.children [
                        TextBlock.create [
                            Grid.column 0
                            TextBlock.text notification
                        ]
                    ]
                ]
            ]
        ]

    let private notificationsView (notifications: string list) =
        match notifications with
        | notifications when notifications.Length > 0 ->
            StackPanel.create [
                StackPanel.dock Dock.Top
                StackPanel.classes [ "dark" ]
                StackPanel.children [
                    ItemsControl.create [
                        ItemsControl.dataItems notifications
                        ItemsControl.itemTemplate
                            (DataTemplateView<string>.create
                                notificationItemView)
                    ]
                ]
            ]
        | _ ->
            StackPanel.create [
                StackPanel.dock Dock.Top
            ]

    let private renderButtonBar state dispatch =
        StackPanel.create [
            StackPanel.dock Dock.Top
            StackPanel.margin 10.0
            StackPanel.orientation Orientation.Horizontal
            StackPanel.spacing 5.0
            StackPanel.children [
                Button.create [
                    Button.content "Install game"
                    Button.onClick (fun _ -> OpenInstallGameWindow |> dispatch)
                ]
                Button.create [
                    Button.content "Upgrade games"
                    Button.onClick (fun _ -> UpgradeGames |> dispatch)
                ]
            ]
        ]

    let private renderTerminalOutput state dispatch =
        TextBox.create [
            TextBox.dock Dock.Bottom
            TextBox.height 100.0
            TextBox.isReadOnly true
            TextBox.text state.terminalOutput
        ]

    let private mainAreaView (state: State) dispatch =
        DockPanel.create [
            DockPanel.column 1
            DockPanel.horizontalAlignment HorizontalAlignment.Stretch
            DockPanel.verticalAlignment VerticalAlignment.Stretch
            DockPanel.children [
                notificationsView state.notifications
                renderButtonBar state dispatch
                renderTerminalOutput state dispatch
                ScrollViewer.create [
                    ScrollViewer.horizontalScrollBarVisibility
                        ScrollBarVisibility.Disabled
                    ScrollViewer.padding 10.0
                    ScrollViewer.content (renderGameList state dispatch)
                ]
            ]
        ]

    let private iconBarView state dispatch =
        StackPanel.create [
            StackPanel.dock Dock.Top
            StackPanel.margin (Thickness.Parse("0, 10"))
            StackPanel.orientation Orientation.Horizontal
            StackPanel.children [
                Button.create [
                    Button.classes [ "iconButton" ]
                    Button.content Icons.settings
                    Button.onClick (fun _ -> OpenSettings |> dispatch)
                ]
            ]
        ]

    // TODO: icon
    let private menuItem dispatch currentMode (text: string) badge mode =
        Button.create [
            Button.classes [
                if mode = currentMode then "active" else ()
            ]
            Button.content
                (DockPanel.create [
                    DockPanel.children [
                        match badge with
                        | Some badge ->
                            Border.create [
                                Border.classes [ "badge" ]
                                Border.dock Dock.Right
                                Border.margin (5.0, 0.0, 0.0, 0.0)
                                Border.child
                                    (TextBlock.create [
                                        TextBlock.text (badge |> string)
                                     ])
                            ]
                        | None -> ()
                        TextBlock.create [
                            TextBlock.text text
                            TextBlock.verticalAlignment VerticalAlignment.Center
                        ]
                    ]
                 ])
            Button.onClick (fun _ -> ChangeMode mode |> dispatch)
        ]

    let private middleView state dispatch =
        let menuItem = menuItem dispatch state.mode

        ScrollViewer.create [
            ScrollViewer.content
                (StackPanel.create [
                    StackPanel.orientation Orientation.Vertical
                    StackPanel.children [
                        menuItem
                            "Installed"
                            (Map.count state.installedGames |> Some)
                            Installed
                    ]
                 ])
        ]

    let private downloadTemplateView (downloadStatus: DownloadStatus) =
        Grid.create [
            Grid.columnDefinitions "Auto"
            Grid.margin (0.0, 5.0)
            Grid.rowDefinitions "Auto, Auto, Auto"
            Grid.children [
                TextBlock.create [
                    TextBlock.row 0
                    TextBlock.text downloadStatus.gameTitle
                ]
                ProgressBar.create [
                    Grid.row 1
                    ProgressBar.isVisible
                    <| not downloadStatus.installing
                    ProgressBar.maximum downloadStatus.fileSize
                    ProgressBar.value
                    <| double downloadStatus.downloaded
                ]
                TextBlock.create [
                    Grid.row 2
                    TextBlock.isVisible downloadStatus.installing
                    TextBlock.text "Installing..."
                ]
                TextBlock.create [
                    Grid.row 2
                    TextBlock.isVisible
                    <| not downloadStatus.installing
                    TextBlock.text
                    <| sprintf
                        "%i MB / %i MB"
                        downloadStatus.downloaded
                        (int downloadStatus.fileSize)
                ]
            ]
        ]

    let private downloadsView (downloadList: DownloadStatus list) =
        StackPanel.create [
            StackPanel.orientation Orientation.Vertical
            StackPanel.margin (Thickness.Parse "12, 12")
            StackPanel.children [
                ItemsControl.create [
                    ItemsControl.dataItems downloadList
                    ItemsControl.itemTemplate
                        (DataTemplateView<DownloadStatus>.create
                            downloadTemplateView)
                ]
                TextBlock.create [
                    TextBlock.isVisible (downloadList.Length = 0)
                    TextBlock.text "No downloads"
                ]
            ]
        ]

    let private bottomBarView state dispatch =
        StackPanel.create [
            StackPanel.dock Dock.Bottom
            StackPanel.orientation Orientation.Vertical
            StackPanel.children [
                downloadsView state.downloads
                TextBlock.create [
                    TextBlock.dock Dock.Bottom
                    TextBlock.fontSize 10.0
                    TextBlock.text Config.version
                ]
            ]
        ]

    let renderLeftBar state dispatch =
        Border.create [
            Border.classes [ "leftBar" ]
            Border.column 0
            Border.padding (5.0, 0.0)
            Border.child
                (DockPanel.create [
                    DockPanel.children [
                        iconBarView state dispatch
                        bottomBarView state dispatch
                        middleView state dispatch
                    ]
                 ])
        ]

    let render state dispatch: IView =
        DockPanel.create [
            DockPanel.verticalAlignment VerticalAlignment.Stretch
            DockPanel.horizontalAlignment HorizontalAlignment.Stretch
            DockPanel.lastChildFill true
            DockPanel.children [
                Grid.create [
                    Grid.columnDefinitions "1*, 3*"
                    Grid.children [
                        renderLeftBar state dispatch :> IView
                        mainAreaView state dispatch :> IView
                    ]
                ]
            ]
        ]
        :> IView
