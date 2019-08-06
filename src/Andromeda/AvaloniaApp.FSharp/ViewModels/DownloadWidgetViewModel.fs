namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.Installed
open GogApi.DotNet.FSharp.Base
open ReactiveUI.Legacy
open System
open System.Collections.Generic
open System.ComponentModel
open System.Diagnostics
open System.IO

open Andromeda.AvaloniaApp.FSharp.Helpers
open Andromeda.AvaloniaApp.FSharp.ViewModels

type DownloadWidgetViewModel(control, appDataWrapper) as this =
    inherit SubViewModelBase(control, appDataWrapper)

    let downloadQueue = Queue<InstallationInfos>()
    let downloads = ReactiveList<DownloadStatus>()

    member __.Init () =
        base.Init()
        this.UpgradeAllGames ()
        let timer = new Timers.Timer(1.0 * 3600.0 * 1000.0);
        timer.AutoReset <- true
        timer.Elapsed.Add(fun (_) -> this.UpgradeAllGames())
        timer.Start ()

    member val Downloads = downloads

    member __.UpgradeAllGames () =
        match this.AppData.authentication with
        | NoAuth -> ()
        | Auth _ ->
            this.AppData |> searchInstalled |> this.SetAppData
            let (list, appData) = this.AppData |> checkAllForUpdates
            appData |> this.SetAppData

            let message = "Found " + list.Length.ToString() + " games to update."
            message |> Logger.LogInfo // Move logging to Notification handling
            this.GetRootViewModel().AddNotification(message)

            for updateInfo in list do
                let games = this.AppData.installedGames |> List.where (fun game -> game.id = updateInfo.game.id)
                games.Length > 0 |> Debug.Assert
                // TODO: Select if multiple hits
                let game::_ = games
                match updateInfo.newVersion = game.version with
                | true -> ()
                | false ->
                    let (installerInfos, appData) = Games.getAvailableInstallersForOs appData game.id
                    appData |> this.SetAppData
                    // TODO: Select if multiple hits
                    let installerInfo::_ = installerInfos
                    InstallationInfos (game.name, installerInfo) |> this.AddDownload

    member this.AddDownload info =
        info |> downloadQueue.Enqueue
        "Added download of " + info.GameTitle + " to download queue." |> Logger.LogInfo

        this.CheckForNewDownload ()

    member this.CheckForNewDownload () =
        match downloadQueue.Count > 0 with
        | true -> downloadQueue.Dequeue() |> this.StartDownload
        | false -> ()

    member this.StartDownload info =
        "Get download info for " + info.GameTitle + " to download queue." |> Logger.LogInfo
        match Games.downloadGame this.AppData info.GameTitle info.InstallerInfo with
        | None -> ()
        | Some (task, filepath, tmppath, size) ->
            let downloadInfo = DownloadStatus(info.GameTitle, tmppath, float(size) / 1000000.0)
            this.Downloads.Add(downloadInfo)

            let taskData =
                match task with
                | Some task ->
                    "Download installer for " + info.GameTitle + "." |> Logger.LogInfo
                    let timer = new Timers.Timer (500.0)
                    timer.AutoReset <- true
                    timer.Elapsed.Add(fun _ ->
                        downloadInfo.FilePath
                        |> FileInfo
                        |> fun fileInfo -> int(float(fileInfo.Length) / 1000000.0)
                        |> downloadInfo.UpdateDownloaded
                    )
                    timer.Start()
                    Some (task, timer)
                | None ->
                    "Use cached installer for " + info.GameTitle + "." |> Logger.LogInfo
                    None

            let worker = new BackgroundWorker ()
            worker.DoWork.Add(fun _ ->
                match taskData with
                | Some (downloadTask, timer) ->
                    downloadTask.Wait()
                    File.Move(tmppath, filepath)
                    timer.Stop()
                | None -> ()

                downloadInfo.FilePath <- filepath

                // Install game
                downloadInfo.IndicateInstalling()
                "Unpack " + downloadInfo.GameTitle + " from " + downloadInfo.FilePath |> Logger.LogInfo
                Games.extractLibrary this.AppData downloadInfo.GameTitle downloadInfo.FilePath
                downloadInfo.GameTitle + " unpacked successfully!" |> Logger.LogInfo
            )
            worker.RunWorkerCompleted.Add(fun _ ->
                downloadInfo |> this.Downloads.Remove |> ignore
                searchInstalled this.AppData |> this.SetAppData
                "Cleaned up after install." |> Logger.LogInfo
            )
            worker.RunWorkerAsync ()
