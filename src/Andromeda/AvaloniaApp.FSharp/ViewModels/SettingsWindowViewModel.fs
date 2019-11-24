namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.Core.FSharp
open Avalonia.Controls
open ReactiveUI
open System.IO
open System.Reactive
open System.Reactive.Linq

type SettingsWindowViewModel(control, parent) as this =
    inherit SubViewModelBase(control, parent)

    let mutable gamePath = this.AppDataWrapper.AppData.settings.gamePath
    let mutable invalid: ObservableAsPropertyHelper<bool> = null

    member this.GamePath
        with private get () = gamePath
        and private set value =
            this.RaiseAndSetIfChanged(&gamePath, value) |> ignore
    member __.Invalid = invalid.Value

    member val SaveCommand: ReactiveCommand<Window, Unit> = ReactiveCommand.Create<Window>(this.Save)

    // Necessary, because F# wants to initialize EVERYTHING before using ANYTHING...
    member __.Initialize() =
        invalid <-
            this.WhenAnyValue<SettingsWindowViewModel, string>(fun (x: SettingsWindowViewModel) -> x.GamePath)
                .Select(Directory.Exists >> not)
                .ToProperty(this, (fun (x: SettingsWindowViewModel) -> x.Invalid))

    member this.Save(window: Window) =
        let appData = this.AppDataWrapper.AppData
        if this.GamePath |> Directory.Exists then
            if appData.settings.gamePath <> this.GamePath then
                let appData = { this.AppDataWrapper.AppData with settings = { Settings.gamePath = this.GamePath } }
                this.AppDataWrapper.AppData <- Installed.searchInstalled AppDataPersistence.save appData
            else
                ()
            window.Close()
        else
            // Prevent saving, if Path is invalid
            ()
