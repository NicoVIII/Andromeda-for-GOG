namespace Andromeda.AvaloniaApp.ViewComponents

open Avalonia.Controls
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open System

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
        TextBox.create [
            TextBox.dock Dock.Bottom
            TextBox.height 100.0
            TextBox.isReadOnly true
            TextBox.text (
                state.terminalOutput
                |> String.concat Environment.NewLine
            )
        ]
