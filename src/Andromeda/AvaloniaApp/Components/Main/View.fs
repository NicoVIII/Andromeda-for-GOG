namespace Andromeda.AvaloniaApp.Components.Main

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout

open Andromeda.AvaloniaApp.Components.Main.ViewComponents

module View =
    let render state dispatch: IView =
        DockPanel.create [
            DockPanel.verticalAlignment VerticalAlignment.Stretch
            DockPanel.horizontalAlignment HorizontalAlignment.Stretch
            DockPanel.lastChildFill true
            DockPanel.children [
                Grid.create [
                    Grid.columnDefinitions "1*, 3*"
                    Grid.children [
                        LeftBar.render state dispatch :> IView
                        Main.mainAreaView state dispatch :> IView
                    ]
                ]
            ]
        ]
        :> IView
