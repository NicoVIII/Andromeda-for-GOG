namespace Andromeda.AvaloniaApp.Elements

open Avalonia.Controls
open Avalonia.FuncUI.DSL

module SimpleDockPanel =
    let inline create children =
        DockPanel.create [
            DockPanel.children children
        ]
