namespace Andromeda.AvaloniaApp

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Layout

open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types

open Andromeda.AvaloniaApp.Components
open Andromeda.AvaloniaApp.Elements
open Andromeda.AvaloniaApp.ViewComponents

module View =
    let renderMain state dispatch : IView =
        SimpleDockPanel.create [
            Main.renderButtonBar state dispatch
            Main.renderTerminalOutput state dispatch
            ScrollViewer.create [
                ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Disabled
                ScrollViewer.padding 10.0
                ScrollViewer.content (GameList.render state dispatch)
            ]
        ]

    let renderNotificationItemView (notification: string) =
        StackPanel.create [
            StackPanel.classes [ "info" ]
            StackPanel.children [
                Grid.create [
                    Grid.columnDefinitions "1*, Auto"
                    Grid.margin 6.0
                    Grid.children [
                        TextBlock.create [
                            Grid.column 0
                            TextBlock.text notification
                        ]
                    ]
                ]
            ]
        ]

    let renderNotificationsView (notifications: string list) =
        match notifications with
        | notifications when notifications.Length > 0 ->
            StackPanel.create [
                StackPanel.dock Dock.Top
                StackPanel.classes [ "dark" ]
                StackPanel.children [
                    ItemsControl.create [
                        ItemsControl.dataItems notifications
                        ItemsControl.itemTemplate (
                            DataTemplateView<string>.create renderNotificationItemView
                        )
                    ]
                ]
            ]
        | _ ->
            StackPanel.create [
                StackPanel.dock Dock.Top
            ]

    let renderAuthenticated state dispatch : IView =
        let contextRender =
            match state.context with
            | Installed -> renderMain state dispatch
            // Pages without own navigation item
            | InstallGame subState ->
                let installedGames = state.installedGames |> Map.toList |> List.map fst

                InstallGame.View.render
                    installedGames
                    subState
                    (InstallGameMgs >> dispatch)
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
                            DockPanel.children [
                                DockPanel.create [
                                    DockPanel.column 1
                                    DockPanel.horizontalAlignment
                                        HorizontalAlignment.Stretch
                                    DockPanel.verticalAlignment VerticalAlignment.Stretch
                                    DockPanel.children [
                                        renderNotificationsView state.notifications
                                        contextRender
                                    ]
                                ]
                            ]
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
