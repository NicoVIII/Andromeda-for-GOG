namespace Andromeda.AvaloniaApp.Components.Main

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

open Andromeda.AvaloniaApp

module Update =
    module Subs =
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

    /// Contains everything for the update method, which is a little longer
    module Update =
        /// Checks a single game for an upgrade
        let upgradeGame state game =
            let (updateData, authentication) =
                (Optic.get StateL.authentication state, game)
                ||> Installed.checkGameForUpdate

            // Update authentication in state, if it was refreshed
            let state = Optic.set StateL.authentication authentication state

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

            state, cmd, DoNothing

        /// Sets the local image path for a game
        let setGameImage state productId imgPath =
            let state =
                state
                |> Optic.map
                    StateL.installedGames
                    (Map.change productId (function
                        | Some game -> { game with image = imgPath |> Some } |> Some
                        | None -> None))

            state, Cmd.none, DoNothing

        let addNotification state notification =
            // Remove notification again after 5 seconds
            let removeNotification notification =
                async {
                    do! Async.Sleep 5000
                    return notification
                }

            let state =
                state
                |> Optic.map StateL.notifications (List.append [ notification ])

            let cmd =
                Cmd.OfAsync.perform removeNotification notification RemoveNotification

            state, cmd, DoNothing

        let searchInstalled state =
            let authentication = Optic.get StateL.authentication state

            let (installedGames, imgJobs) =
                (Optic.get StateL.settings state, authentication)
                ||> Installed.searchInstalled

            let state = Optic.set StateL.installedGames installedGames state

            let cmd =
                imgJobs
                |> List.map (fun job ->
                    Cmd.OfAsync.perform job authentication SetGameImage)
                |> Cmd.batch

            state, cmd, DoNothing

        let startGameDownload state (productInfo: ProductInfo) (dlcs: Dlc list) =
            // TODO: download and install DLCs
            let authentication = Optic.get StateL.authentication state

            let installerInfoList =
                Diverse.getAvailableInstallersForOs productInfo.id authentication
                |> Async.RunSynchronously

            match installerInfoList with
            | [] ->
                let cmd = Cmd.ofMsg (AddNotification "Found no installer for this OS...")

                state, cmd, DoNothing
            | [ installerInfo ] ->
                let result =
                    Download.downloadGame productInfo.title installerInfo authentication
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

                    let settings = Optic.get StateL.settings state

                    let state =
                        Optic.map
                            StateL.downloads
                            (Map.add downloadInfo.gameId downloadInfo)
                            state

                    let downloadCmd =
                        match task with
                        | Some task ->
                            let invoke () =
                                async {
                                    let! _ = Async.AwaitTask task
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

                    state, cmd, DoNothing
            | _ ->
                let cmd =
                    Cmd.ofMsg (
                        AddNotification
                            "Found multiple installers, this is not supported yet..."
                    )

                state, cmd, DoNothing

    let update msg (state: State) =
        let justChangeState change = change state, Cmd.none, DoNothing

        match msg with
        | ChangeState change -> justChangeState change
        | ChangeMode mode -> justChangeState (Optic.set StateL.mode mode)
        | StartGame installedGame -> state, Subs.startGame installedGame, DoNothing
        | UpgradeGame game -> Update.upgradeGame state game
        | SetGameImage (productId, imgPath) -> Update.setGameImage state productId imgPath
        | AddNotification notification -> Update.addNotification state notification
        | RemoveNotification notification ->
            let state =
                Optic.map
                    StateL.notifications
                    (List.filter (fun n -> n <> notification))
                    state

            state, Cmd.none, DoNothing
        | AddToTerminalOutput newLine ->
            // Add new line to the front of the terminal
            let state =
                state
                // Limit output to 1000 lines
                |> Optic.map StateL.terminalOutput (fun lines -> newLine :: lines.[..999])

            state, Cmd.none, DoNothing
        | SearchInstalled initial ->
            // TODO: Async?
            let (state, cmd, intent) = Update.searchInstalled state

            let cmd' =
                match initial && state.settings.updateOnStartup with
                | true -> [ cmd; UpgradeGames |> Cmd.ofMsg ] |> Cmd.batch
                | false -> cmd

            (state, cmd', intent)
        | CacheCheck ->
            let cacheCheck () =
                async { do Optic.get StateL.settings state |> Cache.check }

            let cmd =
                Cmd.OfAsync.attempt cacheCheck () (fun _ ->
                    AddNotification "Cachecheck failed!")

            state, cmd, DoNothing
        | SetSettings settings ->
            Persistence.Settings.save settings |> ignore

            let state = Optic.set StateL.settings settings state

            let cmd =
                [ Cmd.ofMsg (SearchInstalled false)
                  Cmd.ofMsg CacheCheck ]
                |> Cmd.batch

            state, cmd, DoNothing
        | FinishGameDownload gameId ->
            let state =
                state
                |> Optic.map StateL.downloads (Map.change gameId (fun _ -> None))

            state, Cmd.ofMsg (SearchInstalled false), DoNothing
        | StartGameDownload (productInfo, dlcs, authentication) ->
            // This is triggered by the parent component, authentication could have changed,
            // so we update it
            let state = Optic.set StateL.authentication authentication state

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

            state, cmd, DoNothing
        | UpgradeGames ->
            // TODO: refactor into single "UpgradeGame" commands for every game
            let (updateDataList, authentication) =
                (Optic.get StateL.installedGames state,
                 Optic.get StateL.authentication state)
                ||> Installed.checkAllForUpdates

            // Update authentication in state, if it was refreshed
            let state = Optic.set StateL.authentication authentication state

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

            state, cmd, DoNothing
        | UpdateDownloadSize (gameId, fileSize) ->
            let state =
                state
                |> Optic.map
                    StateL.downloads
                    (Map.change
                        gameId
                        (Option.map (fun download ->
                            { download with downloaded = fileSize })))

            state, Cmd.none, DoNothing
        | UpdateDownloadInstalling gameId ->
            let state =
                state
                |> Optic.map
                    StateL.downloads
                    (Map.change
                        gameId
                        (Option.map (fun download -> { download with installing = true })))

            state, Cmd.none, DoNothing
        // Intents
        | OpenSettings -> state, Cmd.none, Intent.OpenSettings
        | OpenInstallGameWindow -> state, Cmd.none, Intent.OpenInstallGameWindow
