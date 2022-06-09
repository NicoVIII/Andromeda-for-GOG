namespace Andromeda.AvaloniaApp.ViewComponents

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open SimpleOptics
open System

open Andromeda.Core

open Andromeda.AvaloniaApp

module Main =
    let renderButtonBar state dispatch =
        StackPanel.create [
            StackPanel.dock Dock.Top
            StackPanel.margin 10.0
            StackPanel.orientation Orientation.Horizontal
            StackPanel.spacing 5.0
            StackPanel.children [
                Button.create [
                    Button.content "Install game"
                    Button.onClick (fun _ -> ShowInstallGame |> dispatch)
                ]
                Button.create [
                    Button.content "Upgrade games"
                    Button.onClick (fun _ -> UpgradeGames true |> dispatch)
                ]
            ]
        ]

    let renderTerminalOutput state dispatch =
        TabControl.create [
            TabControl.dock Dock.Bottom
            TabControl.height 130
            TabControl.tabStripPlacement Dock.Bottom
            TabControl.viewItems [
                for KeyValue (productId, output) in state.terminalOutput do
                    let gameName =
                        Optic.get (MainStateOptic.game productId) state
                        |> function
                            | Some game -> GameName.unwrap game.name
                            | None -> "<deleted>"

                    TabItem.create [
                        TabItem.header gameName
                        TabItem.content (
                            TextBox.create [
                                TextBox.isReadOnly true
                                TextBox.text (output |> String.concat Environment.NewLine)
                            ]
                        )
                    ]
            ]
        ]
