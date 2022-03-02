namespace Andromeda.AvaloniaApp

open Avalonia.Controls
open Avalonia.Layout

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types

open Andromeda.AvaloniaApp.Components
open Andromeda.AvaloniaApp.ViewComponents

module View =
    let renderAuthenticated state dispatch =
        let contextRender =
            match state.context with
            | Installed -> Main.View.render state.main (MainMsg >> dispatch)
            | Settings state -> Settings.View.render state (SettingsMsg >> dispatch)

        DockPanel.create [
            DockPanel.verticalAlignment VerticalAlignment.Stretch
            DockPanel.horizontalAlignment HorizontalAlignment.Stretch
            DockPanel.lastChildFill true
            DockPanel.children [
                Grid.create [
                    Grid.columnDefinitions "1*, 3*"
                    Grid.children [
                        LeftBar.render state dispatch
                        DockPanel.create [
                            DockPanel.column 1
                            DockPanel.row 0
                            DockPanel.children [ contextRender ]
                        ]
                    ]
                ]
            ]
        ]

    let render state dispatch : IView =
        match state with
        | Authenticated state -> renderAuthenticated state (Auth >> dispatch)
        | Unauthenticated state ->
            Authentication.render state (AuthenticationMsg >> UnAuth >> dispatch)
