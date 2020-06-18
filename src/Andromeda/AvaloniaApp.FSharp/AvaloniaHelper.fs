namespace Andromeda.AvaloniaApp.FSharp

open Avalonia
open Avalonia.Controls
open Avalonia.Platform
open Avalonia.FuncUI.DSL
open System

module AvaloniaHelper =
    let loadAssetPath (path: string) =
        let uri =
            if path.StartsWith("/")
            then Uri(path, UriKind.Relative)
            else Uri(path, UriKind.RelativeOrAbsolute);

        let assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        assets.Open(uri)

    let simpleTextBlock text =
        TextBlock.create [
            TextBlock.text text
        ]

[<AutoOpen>]
module AvaloniaCefBrowser =
    open Avalonia.FuncUI.Builder
    open Avalonia.FuncUI.Types
    open Xilium.CefGlue.Avalonia

    let create (attrs: IAttr<AvaloniaCefBrowser> list): IView<AvaloniaCefBrowser> =
        ViewBuilder.Create<AvaloniaCefBrowser>(attrs)

    type ContextMenu with
        end
