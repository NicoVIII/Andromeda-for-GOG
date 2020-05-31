namespace Andromeda.AvaloniaApp.FSharp

open DomainTypes

open Andromeda.Core.FSharp
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging
open Elmish
open GogApi.DotNet.FSharp.DomainTypes

module Games =
    type State = unit

    type Msg = unit

    let init () = ()

    let update (_: Msg) (_: State) = ()

    let gameTile gDispatch authentication columns (i: int, game: InstalledGame): IView =
        Border.create
            [ Border.borderBrush "#FF333333"
              Border.borderThickness 1.0
              Border.column (i % columns * 2)
              Border.contextMenu
                  (ContextMenu.create
                      [ ContextMenu.viewItems
                          [ MenuItem.create
                              [ MenuItem.header "Start"
                                MenuItem.onClick (fun _ ->
                                    game |> Global.StartGame |> gDispatch) ] ] ])
              Border.cornerRadius 5.0
              Border.row (i / columns * 2)
              Border.child
                  (Image.create
                      [ Image.source
                          (new Bitmap(Games.getProductImg game.id authentication
                                      |> Async.RunSynchronously)) ]) ] :> IView

    let view gDispatch games authentication =
        let columns = 4
        let gap = 5 |> string
        Grid.create
            [ Grid.columnDefinitions
                ((columns, "1*")
                 ||> List.replicate
                 |> List.reduce (fun a b -> a + " " + gap + " " + b))
              Grid.rowDefinitions
                  ((List.length games / columns + 1, "1*")
                   ||> List.replicate
                   |> List.reduce (fun a b -> a + " " + gap + " " + b))
              Grid.children
                  (games
                   |> List.indexed
                   |> List.map (gameTile gDispatch authentication columns)) ]
