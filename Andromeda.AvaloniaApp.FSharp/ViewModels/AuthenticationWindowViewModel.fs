namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.AvaloniaApp.FSharp.Helpers
open Andromeda.Core.FSharp.AppData
open Avalonia.Controls
open GogApi.DotNet.FSharp.Authentication
open ReactiveUI
open DynamicData
open System.Reactive

type AuthenticationWindowViewModel(appDataWrapper) as this =
    inherit ViewModelBase(appDataWrapper)

    member private this.code = ""
    member this.Code
        with get() = this.code
        and set (value: string) =
            this.RaiseAndSetIfChanged(ref this.code, value) |> ignore

    member this.Authenticate(window: Window) =
        this.AppData <- { this.AppData with authentication = newToken this.Code }
        saveAppData(this.AppData);
        window.Close();

    member this.AuthenticateCommand: ReactiveCommand<Window, Unit> = ReactiveCommand.Create<Window>(this.Authenticate)

