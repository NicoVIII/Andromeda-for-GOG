namespace Andromeda.AvaloniaApp.FSharp

open DomainTypes

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.DomainTypes
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Media
open Avalonia.Media.Imaging

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
                                    game |> Global.StartGame |> gDispatch), OnChangeOf game) ] ] ])
              Border.child
                  (Image.create
                      [ Image.height 120.0
                        Image.stretch Stretch.UniformToFill
                        Image.width 200.0
                        Image.source
                            // TODO: Load Images asynchronously
                            (new Bitmap(Diverse.getProductImg game.id authentication
                                        |> Async.RunSynchronously)) ]) ] :> IView

    let view gDispatch games authentication: IView =
        WrapPanel.create
            [ WrapPanel.children
                (games
                 |> List.indexed
                 |> List.map (gameTile gDispatch authentication)) ] :> IView
