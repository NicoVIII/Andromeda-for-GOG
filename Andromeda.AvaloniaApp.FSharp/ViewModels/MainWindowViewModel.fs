namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.AvaloniaApp.FSharp.Helpers;
open Andromeda.AvaloniaApp.FSharp.Windows;
open Andromeda.Core.FSharp.AppData;
open Andromeda.Core.FSharp.Installed;
open Avalonia.Controls;
open GogApi.DotNet.FSharp.Base
open ReactiveUI;
open ReactiveUI.Legacy;
open System;
open System.Collections.Generic;
open System.Linq;
open System.Reactive;
open System.Text;

type MainWindowViewModel() as this =
    inherit ViewModelBase()

    do this.AppDataWrapper <- new AppDataWrapper(loadAppData()) |> Some
    do this.AppData <- searchInstalled(this.AppData)
    do
        match this.AppData.authentication with
        | NoAuth ->
            let window = new AuthenticationWindow()
            window.DataContext <- new AuthenticationWindowViewModel(this.AppDataWrapper)
            window.ShowDialog() |> ignore
        | _ -> ()

    member private this.installedGames = new ReactiveList<InstalledGame.T>(this.AppData.installedGames.ToList())
    member this.InstalledGames
        with get() = this.installedGames
        and set value = this.RaiseAndSetIfChanged(ref this.installedGames, value) |> ignore

    member this.OpenInstallWindow() =
        let installWindow = new InstallWindow()
        installWindow.DataContext <- new InstallWindowViewModel(this.AppDataWrapper)
        installWindow.ShowDialog() |> ignore

    member val OpenInstallWindowCommand: ReactiveCommand<Unit, unit> = ReactiveCommand.Create<unit>(this.OpenInstallWindow) with get
