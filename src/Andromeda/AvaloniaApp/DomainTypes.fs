namespace Andromeda.AvaloniaApp

[<AutoOpen>]
module DomainTypes =
    module Game =
        open GogApi.DomainTypes
        open Andromeda.Core.DomainTypes

        let toProductInfo game : ProductInfo = { id = game.id; title = game.name }

    open Avalonia.FuncUI.Components.Hosts
    open System

    type IAndromedaWindow =
        abstract Close: unit -> unit
        abstract CloseWithoutCustomHandler: unit -> unit
        abstract AddClosedHandler: (EventArgs -> unit) -> unit

    type AndromedaWindow() =
        inherit HostWindow()

        let mutable customHandler = []

        let removeAllCustomHandler () =
            customHandler
            |> List.iter (fun (x: IDisposable) -> x.Dispose())

        // Try to avoid loop of Closing -> Close -> Closing -> ...
        interface IAndromedaWindow with
            member this.AddClosedHandler handler =
                let disposable = this.Closed.Subscribe(handler)
                customHandler <- disposable :: customHandler

            member __.Close() = base.Close()

            member __.CloseWithoutCustomHandler() =
                removeAllCustomHandler ()
                base.Close()
