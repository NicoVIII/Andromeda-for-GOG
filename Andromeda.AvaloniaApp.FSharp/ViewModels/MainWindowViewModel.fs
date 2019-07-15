namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.AppData
open Microsoft.FSharp.Linq.RuntimeHelpers
open Mono.Unix.Native
open ReactiveUI
open ReactiveUI.Legacy
open System
open System.Collections.Generic
open System.Diagnostics
open System.Linq
open System.Linq.Expressions
open System.Reactive
open System.Reactive.Linq
open System.Threading
open System.Threading.Tasks
open DynamicData

open Andromeda.AvaloniaApp.FSharp.Helpers
open Andromeda.AvaloniaApp.FSharp.Helpers.LinqHelper
open Andromeda.AvaloniaApp.FSharp.ViewModels
open Andromeda.AvaloniaApp.FSharp.Windows

type MainWindowViewModel(window, appDataWrapper) as this =
    inherit ParentViewModelBase(window, appDataWrapper)

    let mutable searchTerm = ""
    let mutable installedGames = ReactiveList<InstalledGame.T> (this.AppData.installedGames)
    let notifications = ReactiveList<NotificationData> ()

    let exp1 = (fun (x:MainWindowViewModel) -> x.InstalledGames) |> toLinq
    let exp2 = (fun (x:MainWindowViewModel) -> x.SearchTerm) |> toLinq

    let filteredInstalledGames =
        this.WhenAnyValue(exp1, exp2)
            (*.Throttle(TimeSpan.FromMilliseconds(800.0))
            .Select(
                fun (installedGames:ReactiveList<InstalledGame.T>, searchTerm:string) ->
                    installedGames.Where(fun i -> searchTerm.Length = 0 || i.name.ToLower().Contains(searchTerm.ToLower()))
            )
            .ToProperty(this, fun (x:MainWindowViewModel) -> x.FilteredInstalledGames);*)

    member val Version = "v0.3.0-alpha.5"

    member this.SearchTerm
        with get () = searchTerm
        and set (value: string) = this.RaiseAndSetIfChanged(&searchTerm, value) |> ignore

    member __.InstalledGames
        with get () = installedGames
        and set (value: ReactiveList<InstalledGame.T>) = this.RaiseAndSetIfChanged(&installedGames, value) |> ignore
    //member __.FilteredInstalledGames = filteredInstalledGames.Value
    member __.Notifications = notifications

    member val DownloadWidgetVM:DownloadWidgetViewModel = DownloadWidgetViewModel(this.GetParentWindow(), this)
    
    member this.Downloads = this.DownloadWidgetVM.Downloads;

    member val OpenInstallWindowCommand = ReactiveCommand.Create<unit>(this.OpenInstallWindow)
    member val StartGameCommand = ReactiveCommand.Create<string>(this.StartGame)
    member val UpgradeAllGamesCommand = ReactiveCommand.Create<unit>(this.DownloadWidgetVM.UpgradeAllGames)

    override this.AddNotification message =
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
