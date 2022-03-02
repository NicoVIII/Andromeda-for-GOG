namespace Andromeda.AvaloniaApp.Components.Main

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout

open Andromeda.AvaloniaApp.Components.Main.ViewComponents

module View =
    let render state dispatch : IView =
        DockPanel.create [
            DockPanel.column 1
            DockPanel.horizontalAlignment HorizontalAlignment.Stretch
            DockPanel.verticalAlignment VerticalAlignment.Stretch
            DockPanel.children [
                Main.notificationsView state.notifications
                Main.renderButtonBar state dispatch
                Main.renderTerminalOutput state dispatch
                ScrollViewer.create [
                    ScrollViewer.horizontalScrollBarVisibility
                        ScrollBarVisibility.Disabled
                    ScrollViewer.padding 10.0
                    ScrollViewer.content (GameList.render state dispatch)
                ]
            ]
        ]
