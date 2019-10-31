namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open ReactiveUI

type SettingsWindowViewModel(control, parent) as __ =
    inherit SubViewModelBase(control, parent)

    let mutable gamePath = ""

    member private this.GamePath
        with get() = gamePath
        and set value = this.RaiseAndSetIfChanged(&gamePath, value) |> ignore
