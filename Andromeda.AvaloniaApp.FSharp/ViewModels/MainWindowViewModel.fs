namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Installed
open GogApi.DotNet.FSharp.Base
open ReactiveUI
open ReactiveUI.Legacy
open System
open System.Collections.Generic
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Threading
open System.Threading.Tasks

open Andromeda.AvaloniaApp.FSharp.Helpers
open Andromeda.AvaloniaApp.FSharp.ViewModels
open Andromeda.AvaloniaApp.FSharp.Windows

type MainWindowViewModel(window, appDataWrapper) as this =
    inherit ParentViewModelBase(window, appDataWrapper)

    let mutable searchTerm = ""
    let installedGames = ReactiveList<InstalledGame.T> (this.AppData.installedGames)
    let notifications = ReactiveList<NotificationData> ()

    member val Version = "v0.3.0-alpha.5"

    member this.SearchTerm
        with get () = searchTerm
        and set (value: string) = this.RaiseAndSetIfChanged(ref searchTerm, value) |> ignore

    // TODO: if this works without setter this can be simplified
    member __.InstalledGames
        with get () = installedGames

    // TODO: if this works without setter this can be simplified
    member __.Notifications
        with get () = notifications

    member val DownloadWidgetVM:DownloadWidgetViewModel = DownloadWidgetViewModel(this.GetParentWindow(), this);

    member val OpenInstallWindowCommand = ReactiveCommand.Create<unit>(this.OpenInstallWindow)
    member val StartGameCommand = ReactiveCommand.Create<string>(this.StartGame)
    member val UpgradeAllGamesCommand = ReactiveCommand.Create<unit>(this.DownloadWidgetVM.UpgradeAllGames)

    member this.AddNotification message =
        let notification = NotificationData(message)
        notification |> this.Notifications.Add

        let scheduler = TaskScheduler.FromCurrentSynchronizationContext()

        let timer = new Timers.Timer(5000.0)
        timer.Elapsed.Add (fun _ ->
            Task.Factory.StartNew(
                (fun () -> this.Notifications.Remove(notification)),
                CancellationToken.None,
                TaskCreationOptions.None,
                scheduler
            ) |> ignore
        )
        timer.Start()

    member this.OpenInstallWindow() =
        let installWindow = InstallWindow()
        installWindow.DataContext <- InstallWindowViewModel (installWindow, this)
        installWindow.ShowDialog(this.Control) |> ignore

    member __.StartGame (path: string) = Games.startGame path

and DownloadWidgetViewModel(control, appDataWrapper) as this =
    inherit SubViewModelBase(control, appDataWrapper)

    let downloadQueue = Queue<InstallationInfos>()
    let downloads = ReactiveList<DownloadStatus>()

    let upgradeAllGames () =
        match this.AppData.authentication with
        | NoAuth -> ()
        | Auth _ ->
            this.AppData |> searchInstalled |> this.SetAppData
            let (list, appData) = this.AppData |> checkAllForUpdates
            appData |> this.SetAppData

            let message = "Found " + list.Length.ToString() + " games to update."
            message |> Logger.LogInfo // Move logging to Notification handling
            (this.GetRootViewModel() :?> MainWindowViewModel).AddNotification(message)

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

    do upgradeAllGames ()
    do (
        let timer = new Timers.Timer(1.0 * 3600.0 * 1000.0);
        timer.AutoReset <- true
        timer.Elapsed.Add(fun (_) -> this.UpgradeAllGames())
        timer.Start ()
    )

    member val Downloads = downloads
    member this.UpgradeAllGames () = upgradeAllGames ()

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
                Installed.searchInstalled this.AppData |> this.SetAppData
                downloadInfo |> this.Downloads.Remove |> ignore
                "Cleaned up after install." |> Logger.LogInfo
            )
            worker.RunWorkerAsync ()
