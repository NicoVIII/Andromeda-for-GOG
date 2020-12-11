namespace Andromeda.AvaloniaApp

open Avalonia
open Avalonia.Controls
open Avalonia.Platform
open Avalonia.FuncUI.DSL
open System

module AvaloniaHelper =
    let loadAssetPath (path: string) =
        let uri =
            if path.StartsWith("/") then
                Uri(path, UriKind.Relative)
            else
                Uri(path, UriKind.RelativeOrAbsolute)

        let assets =
            AvaloniaLocator.Current.GetService<IAssetLoader>()

        assets.Open(uri)

    let simpleTextBlock text = TextBlock.create [ TextBlock.text text ]
