namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.Core.FSharp
open ReactiveUI
open ReactiveUI.Legacy
open System
open System.Reactive.Linq
open System.Threading
open System.Threading.Tasks
open DynamicData

open Andromeda.AvaloniaApp.FSharp.Helpers
open Andromeda.AvaloniaApp.FSharp.ViewModels
open Andromeda.AvaloniaApp.FSharp.Windows

type MainWindowViewModel(window, appDataWrapper) as this =
    inherit ParentViewModelBase(window, appDataWrapper)

    let mutable searchTerm = ""
    let mutable installedGames: ObservableAsPropertyHelper<InstalledGame list> = null
    let notifications = ReactiveList<NotificationData>()

    let mutable filteredInstalledGames: ObservableAsPropertyHelper<InstalledGame list> = null

    member val Version = "v0.3.0-alpha.5"

    member this.SearchTerm
        with get () = searchTerm
        and set (value: string) = this.RaiseAndSetIfChanged(&searchTerm, value) |> ignore

    member __.InstalledGames = installedGames.Value
    member __.FilteredInstalledGames = filteredInstalledGames.Value
    member __.Notifications = notifications

    member val DownloadWidgetVM: DownloadWidgetViewModel = DownloadWidgetViewModel(this.GetParentWindow(), this)

    member this.Downloads = this.DownloadWidgetVM.Downloads

    member val OpenInstallWindowCommand = ReactiveCommand.Create<unit>(this.OpenInstallWindow)
    member val StartGameCommand = ReactiveCommand.Create<string>(this.StartGame)
    member val UpgradeAllGamesCommand = ReactiveCommand.Create<unit>(this.DownloadWidgetVM.UpgradeAllGames)
    member val OpenSettingsCommand = ReactiveCommand.Create<unit>(this.OpenSettings)

    override this.AddNotification message =
        let notification = NotificationData(message)
        notification |> this.Notifications.Add

        let scheduler = TaskScheduler.FromCurrentSynchronizationContext()

        let timer = new Timers.Timer(5000.0)
        timer.Elapsed.Add
            (fun _ ->
            Task.Factory.StartNew
                ((fun () -> this.Notifications.Remove(notification)), CancellationToken.None, TaskCreationOptions.None,
                 scheduler) |> ignore)
        timer.Start()

    member this.OpenInstallWindow() =
        let installWindow = InstallWindow()
        installWindow.DataContext <- InstallWindowViewModel(installWindow, this)
        installWindow.ShowDialog(this.Control) |> ignore

    member this.OpenSettings() =
        let settingsWindow = SettingsWindow()
        let settingsWindowVM = SettingsWindowViewModel(this.AppDataWrapper)
        settingsWindowVM.Initialize()
        settingsWindow.DataContext <- settingsWindowVM
        settingsWindow.ShowDialog(this.Control) |> ignore

    member __.StartGame(path: string) = Games.startGame path

    // Necessary, because F# wants to initialize EVERYTHING before using ANYTHING...
    member __.Initialize() =
        installedGames <-
            this.WhenAnyValue<MainWindowViewModel, AppData>(fun (x: MainWindowViewModel) -> x.AppDataWrapper.AppData)
                .Select(fun (appData: AppData) -> appData.installedGames)
                .ToProperty(this, (fun (x: MainWindowViewModel) -> x.InstalledGames))

        filteredInstalledGames <-
        this
          .WhenAnyValue<MainWindowViewModel, InstalledGame list, string>(
            (fun (x: MainWindowViewModel) -> x.InstalledGames),
            (fun (x: MainWindowViewModel) -> x.SearchTerm)
          )
          .Throttle(TimeSpan.FromMilliseconds(800.0))
          .Select(fun (installedGames: InstalledGame list, searchTerm: string) ->
            installedGames
            |> List.where (fun i -> searchTerm.Length = 0 || i.name.ToLower().Contains(searchTerm.ToLower()))
          )
          .ToProperty(this, fun (x: MainWindowViewModel) -> x.FilteredInstalledGames)
