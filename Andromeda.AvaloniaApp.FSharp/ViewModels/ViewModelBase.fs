namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.AvaloniaApp.FSharp.Helpers
open Andromeda.Core.FSharp.AppData
open ReactiveUI

type ViewModelBase(appDataWrapper: AppDataWrapper option) as this =
    inherit ReactiveObject()

    new() = ViewModelBase()

    member val AppDataWrapper: AppDataWrapper option = appDataWrapper with get, set
    member this.AppData
        with get(): AppData =
            match this.AppDataWrapper with
            | None -> failwith "AppData is not set yet!"
            | Some wrapper -> wrapper.AppData
        and set (value: AppData) =
            match this.AppDataWrapper with
            | None ->
                failwith "AppDataWrapper is not set yet!"
            | Some wrapper ->
                wrapper.AppData <- value
