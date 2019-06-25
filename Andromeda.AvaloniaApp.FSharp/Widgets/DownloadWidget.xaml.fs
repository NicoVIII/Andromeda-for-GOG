namespace Andromeda.AvaloniaApp.FSharp.Widgets

open Avalonia.Controls
open Avalonia.Markup.Xaml

type DownloadWidget () as this =
    inherit UserControl ()

    let initializeComponent () =
        this |> AvaloniaXamlLoader.Load

    do initializeComponent()
