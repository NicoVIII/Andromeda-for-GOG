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
            | Pending -> Some(0, 1, "Pending...", "")
            | Downloading (current, max, _) ->
                let current, max = (int current, int max)
                // TODO: switch units if applicable
                let text = sprintf "%i MiB / %i MiB" current max
                Some(current, max, "Downloading...", text)
            | Installing _ -> Some(1, 1, "Installing...", "")
            | GameStatus.Installed _ -> None
            | Errored msg -> Some(0, 0, $"Error - {msg}", "")

        let gap = 5.0

        Border.create [
            Border.margin (0.0, 0.0, gap, gap)
            Border.contextMenu (
                ContextMenu.create [
                    ContextMenu.viewItems [
                        match game.status with
                        | GameStatus.Installed (_, gameDir) ->
                            yield!
                                [ MenuItem.create [
                                      MenuItem.header "Start"
                                      MenuItem.onClick (
                                          (fun _ ->
                                              StartGame(game.id, gameDir) |> dispatch),
                                          OnChangeOf(game.id, gameDir)
                                      )
                                  ]
                                  :> IView
                                  MenuItem.create [
                                      MenuItem.header "Update"
                                      MenuItem.onClick (
                                          (fun _ -> UpgradeGame(game, true) |> dispatch),
                                          OnChangeOf game
                                      )
                                  ]
                                  MenuItem.create [ MenuItem.header "-" ]

                                  MenuItem.create [
                                      MenuItem.header "Open game folder"
                                      MenuItem.onClick (
                                          (fun _ -> gameDir |> System.openFolder),
                                          OnChangeOf game
                                      )
                                  ] ]
                        | _ -> ()
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
                        | Some (value, maximum, text, downloadText) ->
                            let progressHeight = 20

                            yield!
                                [ Border.create [
                                      Border.height height
                                      Border.left 0
                                      Border.top 0
                                      Border.width width
                                      Border.background (
                                          SolidColorBrush(Colors.Black, 0.8)
                                      )
                                      Border.child (
                                          TextBlock.create [
                                              TextBlock.horizontalAlignment
                                                  HorizontalAlignment.Center
                                              TextBlock.verticalAlignment
                                                  VerticalAlignment.Center

                                              TextBlock.text text
                                          ]
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
                                              TextBlock.text downloadText
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
