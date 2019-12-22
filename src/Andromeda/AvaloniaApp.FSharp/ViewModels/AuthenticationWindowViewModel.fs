namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.AppData
open Avalonia.Controls
open ReactiveUI
open System.Reactive

type AuthenticationWindowViewModel(control, parent) as this =
    inherit SubViewModelBase(control, parent)

    let mutable code = ""
    member this.Code
        with get() = code
        and set (value: string) =
            this.RaiseAndSetIfChanged(&code, value) |> ignore

    member val AuthenticateCommand: ReactiveCommand<Window, Unit> = ReactiveCommand.Create<Window>(this.Authenticate)

    member this.Authenticate(window: Window) =
        withNewToken this.AppData this.Code
        |> fun x -> AppDataPersistence.save; x
        |> this.SetAppData
        window.Close();
