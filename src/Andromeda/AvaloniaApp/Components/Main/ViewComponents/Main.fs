namespace Andromeda.AvaloniaApp.Components.Main.ViewComponents

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.Layout

open Andromeda.AvaloniaApp.Components.Main

module Main =
    let private notificationItemView (notification: string) =
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

    let private notificationsView (notifications: string list) =
        match notifications with
        | notifications when notifications.Length > 0 ->
            StackPanel.create [
                StackPanel.dock Dock.Top
                StackPanel.classes [ "dark" ]
                StackPanel.children [
                    ItemsControl.create [
                        ItemsControl.dataItems notifications
                        ItemsControl.itemTemplate
                            (DataTemplateView<string>.create
                                notificationItemView)
                    ]
                ]
            ]
        | _ ->
            StackPanel.create [
                StackPanel.dock Dock.Top
            ]

    let private renderButtonBar state dispatch =
        StackPanel.create [
            StackPanel.dock Dock.Top
            StackPanel.margin 10.0
            StackPanel.orientation Orientation.Horizontal
            StackPanel.spacing 5.0
            StackPanel.children [
                Button.create [
                    Button.content "Install game"
                    Button.onClick (fun _ -> OpenInstallGameWindow |> dispatch)
                ]
                Button.create [
                    Button.content "Upgrade games"
                    Button.onClick (fun _ -> UpgradeGames |> dispatch)
                ]
            ]
        ]

    let private renderTerminalOutput state dispatch =
        TextBox.create [
            TextBox.dock Dock.Bottom
            TextBox.height 100.0
            TextBox.isReadOnly true
            TextBox.text state.terminalOutput
        ]

    let mainAreaView (state: State) dispatch =
        DockPanel.create [
            DockPanel.column 1
            DockPanel.horizontalAlignment HorizontalAlignment.Stretch
            DockPanel.verticalAlignment VerticalAlignment.Stretch
            DockPanel.children [
                notificationsView state.notifications
                renderButtonBar state dispatch
                renderTerminalOutput state dispatch
                ScrollViewer.create [
                    ScrollViewer.horizontalScrollBarVisibility
                        ScrollBarVisibility.Disabled
                    ScrollViewer.padding 10.0
                    ScrollViewer.content (GameList.render state dispatch)
                ]
            ]
        ]
