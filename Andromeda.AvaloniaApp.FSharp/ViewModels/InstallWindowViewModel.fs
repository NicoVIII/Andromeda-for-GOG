namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.Core.FSharp.Games
open Andromeda.Core.FSharp.Installed
open ReactiveUI
open DynamicData
open GogApi.DotNet.FSharp.Listing
open System.Diagnostics
open System.Linq
open System.Reactive
open Avalonia.Controls

open Andromeda.AvaloniaApp.FSharp.Helpers

type InstallWindowViewModel(control, parent) as this =
    inherit SubViewModelBase(control, parent)

    let gameSearchTerm = ""
    let foundGames = new SourceList<ProductInfo>()

    member private this.GameSearchTerm
        with get() = gameSearchTerm
        and set value = this.RaiseAndSetIfChanged(ref gameSearchTerm, value) |> ignore

    member private this.FoundGames
        with get() = foundGames
        and set value = this.RaiseAndSetIfChanged(ref foundGames, value) |> ignore

    member this.SearchGame() =
        let (games, appData) = getAvailableGamesForSearch this.AppData this.GameSearchTerm;
        appData |> this.SetAppData
        match games with
        | Some games when List.isEmpty games |> not ->
            //this.FoundGames.AddRange(list);
            let game = games.First()
            let (installers, appData) = getAvailableInstallersForOs this.AppData game.id
            appData |> this.SetAppData
            match List.isEmpty installers |> not with
            | true ->
                let installerInfo = installers.First();
                // TODO: refactor
                let downloadWidgetVM::_ =
                    this.GetRootViewModel().GetChildrenOfType<DownloadWidgetViewModel>()
                downloadWidgetVM.AddDownload(InstallationInfos(game.title, installerInfo))
            | false ->
                let message = "No installer for game found!"
                message |> Logger.LogWarning
                message |> this.GetRootViewModel().AddNotification
        | Some _ | None ->
            let message = "Found no matching game to install."
            message |> Logger.LogWarning
            message |> this.GetRootViewModel().AddNotification
        (this.Control :?> Window).Close()

    member val SearchGameCommand: ReactiveCommand<Unit, unit> = ReactiveCommand.Create<unit>(this.SearchGame)
