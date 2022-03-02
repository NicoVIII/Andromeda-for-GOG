namespace Andromeda.AvaloniaApp.Elements

open Avalonia.Controls
open Avalonia.FuncUI.DSL

module Badge =
    let inline create text =
        Border.create [
            Border.classes [ "badge" ]
            Border.dock Dock.Right
            Border.margin (5.0, 0.0, 0.0, 0.0)
            Border.child (TextBlock.create [ TextBlock.text text ])
        ]
