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
open Elmish
open GogApi.DotNet.FSharp.DomainTypes

module Games =
    type State = unit

    type Msg = unit

    let init () = ()

    let update (_: Msg) (_: State) = ()

    let gameTile (productId: ProductId) : IView =
        TextBlock.create [ TextBlock.text (productId |> string) ] :> IView

    let view games =
        Grid.create
            [ Grid.columnDefinitions "1* 1* 1*"
              Grid.children (games |> List.map gameTile) ]
