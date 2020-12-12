namespace Andromeda.AvaloniaApp.Components.Main.ViewComponents

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.Layout

open Andromeda.AvaloniaApp
open Andromeda.AvaloniaApp.Components.Main

module LeftBar =
    let private iconBarView state dispatch =
        StackPanel.create [
            StackPanel.dock Dock.Top
            StackPanel.margin (Thickness.Parse("0, 10"))
            StackPanel.orientation Orientation.Horizontal
            StackPanel.children [
                Button.create [
                    Button.classes [ "iconButton" ]
                    Button.content Icons.settings
                    Button.onClick (fun _ -> OpenSettings |> dispatch)
                ]
            ]
        ]

    // TODO: icon
    let private menuItem dispatch currentMode (text: string) badge mode =
        Button.create [
            Button.classes [
                if mode = currentMode then "active" else ()
            ]
            Button.content
                (DockPanel.create [
                    DockPanel.children [
                        match badge with
                        | Some badge ->
                            Border.create [
                                Border.classes [ "badge" ]
                                Border.dock Dock.Right
                                Border.margin (5.0, 0.0, 0.0, 0.0)
                                Border.child
                                    (TextBlock.create [
                                        TextBlock.text (badge |> string)
                                     ])
                            ]
                        | None -> ()
                        TextBlock.create [
                            TextBlock.text text
                            TextBlock.verticalAlignment VerticalAlignment.Center
                        ]
                    ]
                 ])
            Button.onClick (fun _ -> ChangeMode mode |> dispatch)
        ]

    let private middleView state dispatch =
        let menuItem = menuItem dispatch state.mode

        ScrollViewer.create [
            ScrollViewer.content
                (StackPanel.create [
                    StackPanel.orientation Orientation.Vertical
                    StackPanel.children [
                        menuItem
                            "Installed"
                            (Map.count state.installedGames |> Some)
                            Installed
                    ]
                 ])
        ]

    let private downloadTemplateView (downloadStatus: DownloadStatus) =
        Grid.create [
            Grid.columnDefinitions "Auto"
            Grid.margin (0.0, 5.0)
            Grid.rowDefinitions "Auto, Auto, Auto"
            Grid.children [
                TextBlock.create [
                    TextBlock.row 0
                    TextBlock.text downloadStatus.gameTitle
                ]
                ProgressBar.create [
                    Grid.row 1
                    ProgressBar.isVisible
                    <| not downloadStatus.installing
                    ProgressBar.maximum downloadStatus.fileSize
                    ProgressBar.value
                    <| double downloadStatus.downloaded
                ]
                TextBlock.create [
                    Grid.row 2
                    TextBlock.isVisible downloadStatus.installing
                    TextBlock.text "Installing..."
                ]
                TextBlock.create [
                    Grid.row 2
                    TextBlock.isVisible
                    <| not downloadStatus.installing
                    TextBlock.text
                    <| sprintf
                        "%i MB / %i MB"
                        downloadStatus.downloaded
                        (int downloadStatus.fileSize)
                ]
            ]
        ]

    let private downloadsView downloadMap =
        let downloadList = Map.toList downloadMap |> List.map snd

        StackPanel.create [
            StackPanel.orientation Orientation.Vertical
            StackPanel.margin (Thickness.Parse "12, 12")
            StackPanel.children [
                ItemsControl.create [
                    ItemsControl.dataItems downloadList
                    ItemsControl.itemTemplate
                        (DataTemplateView<DownloadStatus>.create downloadTemplateView)
                ]
                TextBlock.create [
                    TextBlock.isVisible (downloadList.Length = 0)
                    TextBlock.text "No downloads"
                ]
            ]
        ]

    let private bottomBarView state dispatch =
        StackPanel.create [
            StackPanel.dock Dock.Bottom
            StackPanel.orientation Orientation.Vertical
            StackPanel.children [
                downloadsView state.downloads
                TextBlock.create [
                    TextBlock.dock Dock.Bottom
                    TextBlock.fontSize 10.0
                    TextBlock.text Config.version
                ]
            ]
        ]

    let render state dispatch =
        Border.create [
            Border.classes [ "leftBar" ]
            Border.column 0
            Border.padding (5.0, 0.0)
            Border.child
                (DockPanel.create [
                    DockPanel.children [
                        iconBarView state dispatch
                        bottomBarView state dispatch
                        middleView state dispatch
                    ]
                 ])
        ]
