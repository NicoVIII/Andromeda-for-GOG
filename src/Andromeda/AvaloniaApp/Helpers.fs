namespace Andromeda.AvaloniaApp


module AvaloniaHelper =
    open Avalonia.Controls
    open Avalonia.Platform
    open Avalonia.FuncUI.DSL

    open System

    let loadAssetPath (path: string) =
        let uri =
            if path.StartsWith("/") then
                Uri(path, UriKind.Relative)
            else
                Uri(path, UriKind.RelativeOrAbsolute)

        AssetLoader.Open(uri)

    let simpleTextBlock text =
        TextBlock.create [ TextBlock.text text ]

module ElmishHelper =
    open Elmish

    let cmdOfAsync task arg onSuccess =
        let onError exn = raise exn
        Cmd.OfAsync.either task arg onSuccess onError
