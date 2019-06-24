namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.AvaloniaApp.FSharp.Helpers
open Andromeda.Core.FSharp.Games
open Andromeda.Core.FSharp.Installed
open ReactiveUI
open DynamicData
open GogApi.DotNet.FSharp.Listing
open System.Linq
open System.Reactive

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
        this.AppData <- appData;
        match games with
        | Some games when List.isEmpty games |> not ->
            //this.FoundGames.AddRange(list);
            let game = games.First()
            let (installers, appData) = getAvailableInstallersForOs this.AppData game.id
            this.AppData <- appData;
            match List.isEmpty installers |> not with
            | true ->
                let installerInfo = installers.First();
                let res = downloadGame this.AppData game.title installerInfo
                match res with
                | Some (Some task, filepath, tmppath, size) ->
                    task.Wait()
                    extractLibrary this.AppData game.title filepath
                    this.AppData <- searchInstalled(this.AppData)
                | _ -> ()
            | false -> ()

    member val SearchGameCommand: ReactiveCommand<Unit, unit> = ReactiveCommand.Create<unit>(this.SearchGame)
