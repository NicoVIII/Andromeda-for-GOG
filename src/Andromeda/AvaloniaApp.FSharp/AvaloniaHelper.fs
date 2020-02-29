namespace Andromeda.AvaloniaApp.FSharp

open Avalonia
open Avalonia.Platform
open System

module AvaloniaHelper =
    let loadAssetPath (path: string) =
        let uri =
            if path.StartsWith("/")
            then Uri(path, UriKind.Relative)
            else Uri(path, UriKind.RelativeOrAbsolute);

        let assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        assets.Open(uri)
