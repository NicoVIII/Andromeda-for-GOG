namespace Andromeda.AvaloniaApp.Elements

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types

module SimpleDockPanel =
    let inline create children =
        DockPanel.create [
            DockPanel.children children
        ]

module SimpleScrollViewer =
    let inline create (content: IView) =
        ScrollViewer.create [
            ScrollViewer.content content
        ]
