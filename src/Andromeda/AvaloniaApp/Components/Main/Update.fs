namespace Andromeda.AvaloniaApp.Components.Main

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Andromeda.Core.Lenses
open Avalonia.Threading
open Elmish
open GogApi.DotNet.FSharp.DomainTypes
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

                        AvaloniaScheduler.Instance.Schedule
                            (state, new Func<IScheduler, string, IDisposable>(action))
                        |> ignore

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
                (getl StateL.authentication state, game)
                ||> Installed.checkGameForUpdate

            // Update authentication in state, if it was refreshed
            let state =
                setl StateL.authentication authentication state

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
                    AddNotification $"No new version available for %s{game.name}"
                    |> Cmd.ofMsg

            state, cmd, DoNothing

        /// Sets the local image path for a game
        let setGameImage state productId imgPath =
            let state =
                state
                |> getl StateL.installedGames
                |> Map.change
                    productId
                    (function
                    | Some game -> { game with image = imgPath |> Some } |> Some
                    | None -> None)
                |> setlr StateL.installedGames state

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
                |> getl StateL.notifications
                |> List.append [ notification ]
                |> setlr StateL.notifications state

            let cmd =
                Cmd.OfAsync.perform removeNotification notification RemoveNotification

            state, cmd, DoNothing

        let searchInstalled state =
            let authentication = getl StateL.authentication state

            let (installedGames, imgJobs) =
                (getl StateL.settings state, authentication)
                ||> Installed.searchInstalled

            let state =
                setl StateL.installedGames installedGames state

            let cmd =
                imgJobs
                |> List.map
                    (fun job -> Cmd.OfAsync.perform job authentication SetGameImage)
                |> Cmd.batch

            state, cmd, DoNothing

        let startGameDownload state (productInfo: ProductInfo) =
            let authentication = getl StateL.authentication state

            let installerInfoList =
                Diverse.getAvailableInstallersForOs productInfo.id authentication
                |> Async.RunSynchronously

            match installerInfoList with
            | [] ->
                let cmd =
                    Cmd.ofMsg (AddNotification "Found no installer for this OS...")

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

                    let settings = getl StateL.settings state

                    let state =
                        getl StateL.downloads state
                        |> Map.add downloadInfo.gameId downloadInfo
                        |> setlr StateL.downloads state

                    let downloadCmd =
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
                                        (settings, downloadInfo, installerInfo.version))
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
                    Cmd.ofMsg
                        (AddNotification
                            "Found multiple installers, this is not supported yet...")

                state, cmd, DoNothing

    let update msg (state: State) =
        match msg with
        | ChangeState change -> change state, Cmd.none, DoNothing
        | ChangeMode mode -> setl StateL.mode mode state, Cmd.none, DoNothing
        | StartGame installedGame -> state, Subs.startGame installedGame, DoNothing
        | UpgradeGame game -> Update.upgradeGame state game
        | SetGameImage (productId, imgPath) -> Update.setGameImage state productId imgPath
        | AddNotification notification -> Update.addNotification state notification
        | RemoveNotification notification ->
            let state =
                getl StateL.notifications state
                |> List.filter (fun n -> n <> notification)
                |> setlr StateL.notifications state

            state, Cmd.none, DoNothing
        | AddToTerminalOutput newLine ->
            // Add new line to the front of the terminal
            let state =
                getl StateL.terminalOutput state
                // Limit output to 1000 lines
                |> fun lines -> newLine :: lines.[..999]
                |> setlr StateL.terminalOutput state

            state, Cmd.none, DoNothing
        | SearchInstalled -> Update.searchInstalled state
        | CacheCheck ->
            let cacheCheck () =
                async { do getl StateL.settings state |> Cache.check }

            let cmd =
                Cmd.OfAsync.attempt
                    cacheCheck
                    ()
                    (fun _ -> AddNotification "Cachecheck failed!")

            state, cmd, DoNothing
        | SetSettings settings ->
            Persistence.Settings.save settings |> ignore

            let state = setl StateL.settings settings state

            let cmd =
                [ Cmd.ofMsg SearchInstalled
                  Cmd.ofMsg CacheCheck ]
                |> Cmd.batch

            state, cmd, DoNothing
        | FinishGameDownload gameId ->
            let state =
                getl StateL.downloads state
                |> Map.change gameId (fun _ -> None)
                |> setlr StateL.downloads state

            state, Cmd.ofMsg SearchInstalled, DoNothing
        | StartGameDownload (productInfo, authentication) ->
            // This is triggered by the parent component, authentication could have changed,
            // so we update it
            let state =
                setl StateL.authentication authentication state

            Update.startGameDownload state productInfo
        | UnpackGame (settings, downloadInfo, version) ->
            let invoke () =
                Download.extractLibrary
                    settings
                    downloadInfo.gameTitle
                    downloadInfo.filePath
                    version

            let cmd =
                [ Cmd.ofMsg (UpdateDownloadInstalling downloadInfo.gameId)
                  Cmd.OfAsync.perform
                      invoke
                      ()
                      (fun _ -> FinishGameDownload downloadInfo.gameId) ]
                |> Cmd.batch

            state, cmd, DoNothing
        | UpgradeGames ->
            // TODO: refactor into single "UpgradeGame" commands for every game
            let (updateDataList, authentication) =
                (getl StateL.installedGames state, getl StateL.authentication state)
                ||> Installed.checkAllForUpdates

            // Update authentication in state, if it was refreshed
            let state =
                setl StateL.authentication authentication state

            // Download updated installers or show notification
            let cmd =
                match updateDataList with
                | updateDataList when updateDataList.Length > 0 ->
                    List.map
                        (fun (updateData: Installed.UpdateData) ->
                            updateData.game
                            |> gameToDownloadInfo
                            |> (fun productInfo ->
                                (productInfo, authentication) |> StartGameDownload)
                            |> Cmd.ofMsg)
                        updateDataList
                | _ ->
                    [ AddNotification "No games found to update."
                      |> Cmd.ofMsg ]
                |> Cmd.batch

            state, cmd, DoNothing
        | UpdateDownloadSize (gameId, fileSize) ->
            let state =
                getl StateL.downloads state
                |> Map.change
                    gameId
                    (Option.map (fun download -> { download with downloaded = fileSize }))
                |> setlr StateL.downloads state

            state, Cmd.none, DoNothing
        | UpdateDownloadInstalling gameId ->
            let state =
                getl StateL.downloads state
                |> Map.change
                    gameId
                    (Option.map (fun download -> { download with installing = true }))
                |> setlr StateL.downloads state

            state, Cmd.none, DoNothing
        // Intents
        | OpenSettings -> state, Cmd.none, Intent.OpenSettings
        | OpenInstallGameWindow -> state, Cmd.none, Intent.OpenInstallGameWindow
