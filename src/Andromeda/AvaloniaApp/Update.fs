namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Avalonia.Threading
open Elmish
open GogApi.DomainTypes
open SimpleOptics
open System
open System.Diagnostics
open System.IO
open System.Reactive.Concurrency
open System.Threading.Tasks

open Andromeda.AvaloniaApp.Components
open Andromeda.AvaloniaApp.DomainTypes

module Update =
    module Subs =
        let closeWindow (wind: IAndromedaWindow) =
            let sub dispatch =
                wind.AddClosedHandler(fun _ -> CloseAllWindows |> dispatch)
                |> ignore

            Cmd.ofSub sub

        /// Starts a given game in a subprocess and redirects its terminal output
        let startGame (game: InstalledGame) =
            let createEmptyDisposable () =
                { new IDisposable with
                    member __.Dispose() = () }

            let sub dispatch =
                let showGameOutput _ (outLine: DataReceivedEventArgs) =
                    match outLine.Data with
                    | newLine when String.IsNullOrEmpty newLine -> ()
                    | _ ->
                        let state = outLine.Data

                        let action _ newLine =
                            AddToTerminalOutput newLine |> dispatch
                            createEmptyDisposable ()

                        AvaloniaScheduler.Instance.Schedule(
                            state,
                            new Func<IScheduler, string, IDisposable>(action)
                        )
                        |> ignore

                Installed.startGameProcess showGameOutput game.path
                |> ignore

            Cmd.ofSub sub

        let registerDownloadTimer
            (task: Task<unit> option)
            tmppath
            (downloadInfo: DownloadStatus)
            =
            let sub dispatch =
                match task with
                | Some _ ->
                    "Download installer for "
                    + downloadInfo.gameTitle
                    + "."
                    |> Logger.logInfo

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
                    |> Logger.logInfo

                    UpdateDownloadSize(downloadInfo.gameId, int downloadInfo.fileSize)
                    |> dispatch

            Cmd.ofSub sub

    /// Contains everything for the update method, which is a little longer
    module Update =
        /// Checks a single game for an upgrade
        let upgradeGame state game =
            let (updateData, authentication) =
                (Optic.get MainStateOptic.authentication state, game)
                ||> Installed.checkGameForUpdate

            // Update authentication in state, if it was refreshed
            let state = Optic.set MainStateOptic.authentication authentication state

            // Download updated installers or show notification
            let cmd =
                match updateData with
                | Some updateData ->
                    updateData.game
                    |> gameToDownloadInfo
                    |> (fun productInfo ->
                        // TODO: update DLCs
                        (productInfo, [], authentication)
                        |> StartGameDownload)
                    |> Cmd.ofMsg
                | None ->
                    AddNotification $"No new version available for %s{game.name}"
                    |> Cmd.ofMsg

            state, cmd

        /// Sets the local image path for a game
        let setGameImage state productId imgPath =
            let state =
                state
                |> Optic.map
                    MainStateOptic.installedGames
                    (Map.change productId (function
                        | Some game -> { game with image = imgPath |> Some } |> Some
                        | None -> None))

            state, Cmd.none

        let addNotification state notification =
            // Remove notification again after 5 seconds
            let removeNotification notification =
                async {
                    do! Async.Sleep 5000
                    return notification
                }

            let state =
                state
                |> Optic.map MainStateOptic.notifications (List.append [ notification ])

            let cmd =
                Cmd.OfAsync.perform removeNotification notification RemoveNotification

            state, cmd

        let searchInstalled state =
            let authentication = Optic.get MainStateOptic.authentication state

            let (installedGames, imgJobs) =
                (Optic.get MainStateOptic.settings state, authentication)
                ||> Installed.searchInstalled

            let state = Optic.set MainStateOptic.installedGames installedGames state

            let cmd =
                imgJobs
                |> List.map (fun job ->
                    Cmd.OfAsync.perform job authentication SetGameImage)
                |> Cmd.batch

            state, cmd

        let startGameDownload state (productInfo: ProductInfo) (dlcs: Dlc list) =
            // TODO: download and install DLCs
            let authentication = Optic.get MainStateOptic.authentication state

            let installerInfoList =
                Diverse.getAvailableInstallersForOs productInfo.id authentication
                |> Async.RunSynchronously

            match installerInfoList with
            | [] ->
                let cmd = Cmd.ofMsg (AddNotification "Found no installer for this OS...")

                state, cmd
            | [ installerInfo ] ->
                let result =
                    Download.downloadGame productInfo.title installerInfo authentication
                    |> Async.RunSynchronously

                match result with
                | None -> state, Cmd.none
                | Some (task, filePath, tmppath, size) ->
                    let downloadInfo =
                        DownloadStatus.create
                            productInfo.id
                            (productInfo.title)
                            filePath
                            (float (size) / 1000000.0)

                    let settings = Optic.get MainStateOptic.settings state

                    let state =
                        Optic.map
                            MainStateOptic.downloads
                            (Map.add downloadInfo.gameId downloadInfo)
                            state

                    let downloadCmd =
                        match task with
                        | Some task ->
                            let invoke () =
                                async {
                                    let! _ = task |> Async.AwaitTask
                                    File.Move(tmppath, filePath)
                                }

                            Cmd.OfAsync.perform invoke () (fun _ ->
                                UnpackGame(settings, downloadInfo, installerInfo.version))
                        | None ->
                            UnpackGame(settings, downloadInfo, installerInfo.version)
                            |> Cmd.ofMsg

                    let cmd =
                        [ Subs.registerDownloadTimer task tmppath downloadInfo
                          downloadCmd ]
                        |> Cmd.batch

                    state, cmd
            | _ ->
                let cmd =
                    Cmd.ofMsg (
                        AddNotification
                            "Found multiple installers, this is not supported yet..."
                    )

                state, cmd

    let performAuthenticated msg state mainWindow =
        let inline changeContext context (contextState, cmd) =
            { state with context = context contextState }, cmd

        match msg with
        | StartGame installedGame -> state, Subs.startGame installedGame
        | UpgradeGame game -> Update.upgradeGame state game
        | SetGameImage (productId, imgPath) -> Update.setGameImage state productId imgPath
        | AddNotification notification -> Update.addNotification state notification
        | RemoveNotification notification ->
            let state =
                Optic.map
                    MainStateOptic.notifications
                    (List.filter (fun n -> n <> notification))
                    state

            state, Cmd.none
        | AddToTerminalOutput newLine ->
            // Add new line to the front of the terminal
            let state =
                state
                // Limit output to 1000 lines
                |> Optic.map MainStateOptic.terminalOutput (fun lines ->
                    newLine :: lines.[..999])

            state, Cmd.none
        | SearchInstalled initial ->
            // TODO: Async?
            let state, cmd = Update.searchInstalled state

            let cmd' =
                match initial && state.settings.updateOnStartup with
                | true -> [ cmd; UpgradeGames |> Cmd.ofMsg ] |> Cmd.batch
                | false -> cmd

            state, cmd'
        | CacheCheck ->
            let cacheCheck () =
                async {
                    do
                        Optic.get MainStateOptic.settings state
                        |> Cache.check
                }

            let cmd =
                Cmd.OfAsync.attempt cacheCheck () (fun _ ->
                    AddNotification "Cachecheck failed!")

            state, cmd
        | SetSettings settings ->
            Persistence.Settings.save settings |> ignore

            let state = Optic.set MainStateOptic.settings settings state

            let cmd =
                [ Cmd.ofMsg (SearchInstalled false)
                  Cmd.ofMsg CacheCheck ]
                |> Cmd.batch

            state, cmd
        | FinishGameDownload gameId ->
            let state =
                state
                |> Optic.map MainStateOptic.downloads (Map.change gameId (fun _ -> None))

            state, Cmd.ofMsg (SearchInstalled false)
        | StartGameDownload (productInfo, dlcs, authentication) ->
            // This is triggered by the parent component, authentication could have changed,
            // so we update it
            let state = Optic.set MainStateOptic.authentication authentication state

            Update.startGameDownload state productInfo dlcs
        | UnpackGame (settings, downloadInfo, version) ->
            let invoke () =
                Download.extractLibrary
                    settings
                    downloadInfo.gameTitle
                    downloadInfo.filePath
                    version

            let cmd =
                [ Cmd.ofMsg (UpdateDownloadInstalling downloadInfo.gameId)
                  Cmd.OfAsync.perform invoke () (fun _ ->
                      FinishGameDownload downloadInfo.gameId) ]
                |> Cmd.batch

            state, cmd
        | UpgradeGames ->
            // TODO: refactor into single "UpgradeGame" commands for every game
            let (updateDataList, authentication) =
                (Optic.get MainStateOptic.installedGames state,
                 Optic.get MainStateOptic.authentication state)
                ||> Installed.checkAllForUpdates

            // Update authentication in state, if it was refreshed
            let state = Optic.set MainStateOptic.authentication authentication state

            // Download updated installers or show notification
            let cmd =
                match updateDataList with
                | updateDataList when updateDataList.Length > 0 ->
                    List.map
                        (fun (updateData: Installed.UpdateData) ->
                            updateData.game
                            |> gameToDownloadInfo
                            |> (fun productInfo ->
                                (productInfo, [], authentication)
                                |> StartGameDownload)
                            |> Cmd.ofMsg)
                        updateDataList
                | _ ->
                    [ AddNotification "No games found to update."
                      |> Cmd.ofMsg ]
                |> Cmd.batch

            state, cmd
        | UpdateDownloadSize (gameId, fileSize) ->
            let state =
                state
                |> Optic.map
                    MainStateOptic.downloads
                    (Map.change
                        gameId
                        (Option.map (fun download ->
                            { download with downloaded = fileSize })))

            state, Cmd.none
        | UpdateDownloadInstalling gameId ->
            let state =
                state
                |> Optic.map
                    MainStateOptic.downloads
                    (Map.change
                        gameId
                        (Option.map (fun download -> { download with installing = true })))

            state, Cmd.none
        // Context change
        | ShowInstalled ->
            ((), Cmd.none)
            |> changeContext (fun () -> Installed)
        | ShowInstallGame -> InstallGame.init () |> changeContext InstallGame
        | ShowSettings ->
            Settings.init state.settings
            |> changeContext Settings
        // Child components
        | InstallGameMgs msg ->
            match state.context with
            | InstallGame subState ->
                let installGameState, installGameCmd, intent =
                    InstallGame.update state.authentication msg subState

                let state = { state with context = InstallGame installGameState }

                let intentCmd =
                    match intent with
                    | InstallGame.DoNothing -> Cmd.none
                    | InstallGame.Close (productInfo, dlcs) ->
                        StartGameDownload(productInfo, dlcs, state.authentication)
                        |> Cmd.ofMsg

                let cmd =
                    [ Cmd.map InstallGameMgs installGameCmd
                      intentCmd ]
                    |> Cmd.batch

                state, cmd
            | _ ->
                logError "Got InstallGameMsg although context is not InstallGame."
                state, Cmd.none
        | SettingsMsg msg ->
            match state.context with
            | Settings subState ->
                let settingsState, settingsCmd, intent =
                    Settings.update mainWindow msg subState

                let state = { state with context = Settings settingsState }

                let intentCmd =
                    match intent with
                    | Settings.DoNothing -> Cmd.none
                    | Settings.Save settings ->
                        [ SetSettings settings; ShowInstalled ]
                        |> List.map Cmd.ofMsg
                        |> Cmd.batch

                let cmd =
                    [ Cmd.map SettingsMsg settingsCmd
                      intentCmd ]
                    |> Cmd.batch

                state, cmd
            | _ ->
                logError "Got SettingsMsg although context is not Settings."
                state, Cmd.none

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
