namespace Andromeda.AvaloniaApp

open Avalonia.FuncUI.Components.Hosts
open GogApi.DotNet.FSharp.DomainTypes
open System

[<AutoOpen>]
module DomainTypes =
    type DownloadStatus =
        { gameId: ProductId
          gameTitle: string
          filePath: string
          fileSize: float
          installing: bool
          downloaded: int }

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