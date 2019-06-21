namespace Andromeda.AvaloniaApp.FSharp.Windows

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

type WebWindow() as this =
    inherit Window()
    
    let initializeComponent() =
        AvaloniaXamlLoader.Load(this);

    do initializeComponent()
    do this.AttachDevTools()
