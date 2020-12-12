namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Media
open Avalonia.Media.Imaging

open Andromeda.AvaloniaApp.AvaloniaHelper

module GameList =
    type State = unit

    type Msg = unit

    let init () = ()

    let update (_: Msg) (_: State) = ()

    let gameTile gDispatch authentication (i: int, game: InstalledGame): IView =
        let gap = 5.0
        Border.create
            [ Border.margin (0.0, 0.0, gap, gap)
              Border.contextMenu
                  (ContextMenu.create
                      [ ContextMenu.viewItems
                          [ MenuItem.create
                              [ MenuItem.header "Start"
                                MenuItem.onClick ((fun _ ->
                                    game |> Global.StartGame |> gDispatch), OnChangeOf game) ]
                            MenuItem.create
                              [ MenuItem.header "Update"
                                MenuItem.onClick ((fun _ ->
                                    (authentication, game) |> Global.UpgradeGame |> gDispatch), OnChangeOf game) ]
                            MenuItem.create
                              [ MenuItem.header "-" ]
                            MenuItem.create
                              [ MenuItem.header "Open game folder"
                                MenuItem.onClick ((fun _ ->
                                    game.path |> System.openFolder), OnChangeOf game) ] ] ])
              Border.child
                  (Image.create
                      [ Image.height 120.0
                        Image.stretch Stretch.UniformToFill
                        Image.width 200.0
                        Image.source
                            (match game.image with
                             | Some imgPath -> new Bitmap(imgPath)
                             | None -> new Bitmap(loadAssetPath "avares://Andromeda.AvaloniaApp/Assets/placeholder.jpg")) ]) ] :> IView

    let view gDispatch games authentication: IView =
        WrapPanel.create
            [ WrapPanel.children
                (games
                 |> Map.toList
                 |> List.map snd
                 |> List.indexed
                 |> List.map (gameTile gDispatch authentication)) ] :> IView
