namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.Core.FSharp.DomainTypes
open Avalonia.Controls
open ReactiveUI
open System.Reactive

type SettingsWindowViewModel(control, parent) as this =
    inherit SubViewModelBase(control, parent)

    let mutable gamePath = this.AppDataWrapper.AppData.settings.gamePath

    member this.GamePath
        with private get () = gamePath
        and private set value = this.RaiseAndSetIfChanged(&gamePath, value) |> ignore

    member val SaveCommand: ReactiveCommand<Window, Unit> = ReactiveCommand.Create<Window>(this.Save)

    member this.Save(window: Window) =
        this.AppDataWrapper.AppData <-
            { this.AppDataWrapper.AppData with settings = { Settings.gamePath = this.GamePath } }
        window.Close()
