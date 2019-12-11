namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.Core.FSharp
open Avalonia.Controls
open ReactiveUI

open Andromeda.AvaloniaApp.FSharp.Helpers

[<AbstractClass>]
type ViewModelBase(appDataWrapper: AppDataWrapper) as this =
    inherit ReactiveObject()

    member val Children: SubViewModelBase list = [] with get, set
    member val AppDataWrapper: AppDataWrapper = appDataWrapper with get, set
    member this.AppData
        with get () = this.AppDataWrapper.AppData

    abstract member GetParentWindow: unit -> Window
    abstract member GetRootViewModel: unit -> ParentViewModelBase

    member __.Init () =
        for child in this.Children do
            child.Init()

    member this.SetAppData (appData): unit =
        this.AppDataWrapper.AppData <- appData
        // TODO: error handling
        AppDataPersistence.save appData |> ignore

    member this.GetChildrenOfType<'T when 'T :> SubViewModelBase> () =
        this.Children
        |> List.fold (fun list child ->
            match child with
            | :? 'T as child -> child::list
            | _ -> list
        ) []

and [<AbstractClass>]
    ParentViewModelBase(window: Window, appDataWrapper) as this =
    inherit ViewModelBase(appDataWrapper)

    member val Control = window

    abstract member AddNotification: string -> unit

    override __.GetParentWindow () = window
    override __.GetRootViewModel () = this

// Think about making this generic and add interfaces for ViewModelBase and SubViewModelBase
and [<AbstractClass>]
    SubViewModelBase(control: Control, parent: ViewModelBase) as this =
    inherit ViewModelBase(parent.AppDataWrapper)

    do parent.Children <- this::parent.Children

    member val Control = control
    member val Parent: ViewModelBase = parent

    override this.GetParentWindow () =
        match this.Control with
        | :? Window as window -> window
        | _ -> parent.GetParentWindow ()

    override __.GetRootViewModel () = this.Parent.GetRootViewModel ()
