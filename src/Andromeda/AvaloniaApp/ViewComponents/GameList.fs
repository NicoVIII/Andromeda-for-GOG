namespace Andromeda.AvaloniaApp.ViewComponents

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging
open SimpleOptics

open Andromeda.Core
open Andromeda.Core.DomainTypes

open Andromeda.AvaloniaApp
open Andromeda.AvaloniaApp.AvaloniaHelper

module GameList =
    let gameTile dispatch (game: Game) : IView =
        // Is something going on with this game?
        let inProgress =
            match game.status with
            | Pending -> Some(0, 1, "Pending...")
            | Downloading (current, max) ->
                let current, max = (int current, int max)
                // TODO: switch units if applicable
                let text = sprintf "%i MiB / %i MiB" current max
                Some(current, max, text)
            | Installing -> Some(1, 1, "Installing...")
            | GameStatus.Installed _ -> None

        let gap = 5.0

        Border.create [
            Border.margin (0.0, 0.0, gap, gap)
            Border.contextMenu (
                ContextMenu.create [
                    ContextMenu.viewItems [
                        match inProgress with
                        | Some _ -> ()
                        | None ->
                            yield!
                                [ MenuItem.create [
                                      MenuItem.header "Start"
                                      MenuItem.onClick (
                                          (fun _ -> game |> StartGame |> dispatch),
                                          OnChangeOf game
                                      )
                                  ]
                                  :> IView
                                  MenuItem.create [
                                      MenuItem.header "Update"
                                      MenuItem.onClick (
                                          (fun _ -> game |> UpgradeGame |> dispatch),
                                          OnChangeOf game
                                      )
                                  ]
                                  MenuItem.create [ MenuItem.header "-" ] ]
                        MenuItem.create [
                            MenuItem.header "Open game folder"
                            MenuItem.onClick (
                                (fun _ -> game.path |> System.openFolder),
                                OnChangeOf game
                            )
                        ]
                    ]
                ]
            )
            Border.child (
                let height = 120.0
                let width = 200.0

                Canvas.create [
                    Canvas.height 120
                    Canvas.width 200
                    Canvas.children [
                        Image.create [
                            Image.height height
                            Image.stretch Stretch.UniformToFill
                            Image.width width
                            Image.source (
                                match game.image with
                                | Some imgPath -> new Bitmap(imgPath)
                                | None ->
                                    new Bitmap(
                                        loadAssetPath
                                            "avares://Andromeda.AvaloniaApp/Assets/placeholder.jpg"
                                    )
                            )
                        ]

                        match inProgress with
                        | Some (value, maximum, text) ->
                            let progressHeight = 20

                            yield!
                                [ TextBlock.create [
                                      TextBlock.height height
                                      TextBlock.left 0
                                      TextBlock.top 0
                                      TextBlock.width width

                                      TextBlock.background (
                                          SolidColorBrush(Colors.Black, 0.7)
                                      )
                                  ]
                                  :> IView
                                  ProgressBar.create [
                                      ProgressBar.bottom 0
                                      ProgressBar.height progressHeight
                                      ProgressBar.left 0
                                      ProgressBar.width width

                                      ProgressBar.maximum maximum
                                      ProgressBar.value value
                                  ]
                                  Border.create [
                                      Border.bottom 0
                                      Border.left 0
                                      Border.width width
                                      Border.height progressHeight
                                      Border.padding 5
                                      Border.child (
                                          TextBlock.create [
                                              TextBlock.verticalAlignment
                                                  VerticalAlignment.Center
                                              TextBlock.text text
                                          ]
                                      )
                                  ] ]
                        | None -> ()
                    ]
                ]
            )
        ]
        :> IView

    let render state dispatch =
        WrapPanel.create [
            WrapPanel.children (
                Optic.get MainStateOptic.games state
                |> Map.toList
                |> List.map (snd >> gameTile dispatch)
            )
        ]
