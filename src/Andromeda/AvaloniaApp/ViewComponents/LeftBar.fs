namespace Andromeda.AvaloniaApp.ViewComponents

open Avalonia
open Avalonia.Controls
open Avalonia.Layout

open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types

open Andromeda.AvaloniaApp
open Andromeda.AvaloniaApp.Elements

open System.Reflection

module LeftBar =
    [<RequireQualifiedAccess>]
    module Menu =
        type Item = { text: string; msg: AuthMsg }

        module Item =
            let create text msg = { text = text; msg = msg }

        let items = {| installed = Item.create "Installed" ShowInstalled |}

        // TODO: icon
        let renderItem dispatch active item badge =
            Button.create [
                Button.classes [
                    if active then "active" else ()
                ]
                Button.content (
                    SimpleDockPanel.create [
                        Badge.create badge
                        TextBlock.create [
                            TextBlock.text item.text
                            TextBlock.verticalAlignment VerticalAlignment.Center
                        ]
                    ]
                )
                Button.onClick (fun _ -> item.msg |> dispatch)
            ]

        let render state dispatch : IView list =
            let active =
                match state.context with
                | Installed -> Some items.installed
                // Pages without own navigation item
                | Settings _ -> None
                | InstallGame _ -> None

            let render item =
                renderItem dispatch (active = Some item) item

            [ render items.installed (Map.count state.installedGames |> string) ]

    let private iconBarView dispatch =
        StackPanel.create [
            StackPanel.dock Dock.Top
            StackPanel.margin (Thickness.Parse("0, 10"))
            StackPanel.orientation Orientation.Horizontal
            StackPanel.children [
                Button.create [
                    Button.classes [ "iconButton" ]
                    Button.content Icons.settings
                    Button.onClick (fun _ -> ShowSettings |> dispatch)
                ]
            ]
        ]

    let private middleView state dispatch =
        let menuItem = Menu.renderItem dispatch

        SimpleScrollViewer.create (
            StackPanel.create [
                StackPanel.orientation Orientation.Vertical
                StackPanel.children (Menu.render state dispatch)
            ]
        )

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
                    ItemsControl.itemTemplate (
                        DataTemplateView<DownloadStatus>.create downloadTemplateView
                    )
                ]
                TextBlock.create [
                    TextBlock.isVisible (downloadList.Length = 0)
                    TextBlock.text "No downloads"
                ]
            ]
        ]

    let private bottomBarView downloads =
        let version =
            let assemblyVersion = Assembly.GetEntryAssembly().GetName().Version

            match assemblyVersion with
            | v when v.Major > 0 || v.Minor > 0 -> $"v{v.Major}.{v.Minor}.{v.Build}"
            | _ -> "development build"

        StackPanel.create [
            StackPanel.dock Dock.Bottom
            StackPanel.orientation Orientation.Vertical
            StackPanel.children [
                downloadsView downloads
                TextBlock.create [
                    TextBlock.dock Dock.Bottom
                    TextBlock.fontSize 10.0
                    TextBlock.text version
                ]
            ]
        ]

    let render state dispatch =
        Border.create [
            Border.classes [ "leftBar" ]
            Border.column 0
            Border.padding (5.0, 0.0)
            Border.child (
                SimpleDockPanel.create [
                    iconBarView dispatch
                    bottomBarView state.downloads
                    middleView state dispatch
                ]
            )
        ]
