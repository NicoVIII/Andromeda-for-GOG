namespace Andromeda.AvaloniaApp.Components.Main

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Andromeda.Core.Lenses
open Elmish
open GogApi.DotNet.FSharp.DomainTypes
open System
open System.IO

open Andromeda.AvaloniaApp
open System.Diagnostics
open Avalonia.Threading
open System.Threading.Tasks

module Update =
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
