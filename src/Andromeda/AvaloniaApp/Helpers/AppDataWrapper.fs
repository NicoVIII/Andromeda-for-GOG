namespace Andromeda.AvaloniaApp.FSharp.Helpers

open Andromeda.Core.FSharp
open ReactiveUI

/// <summary>This is a wrapper class for appdata to ensure, that
/// there exists only one reference, which can be changed.</summary>
type AppDataWrapper(appData) =
    inherit ReactiveObject()

    let mutable appData: AppData = appData;

    member this.AppData
        with get() = appData
        and set (value: AppData) =
            this.RaiseAndSetIfChanged(&appData, value) |> ignore
