namespace Andromeda.AvaloniaApp

[<AutoOpen>]
module DomainTypes =
    module Game =
        open GogApi.DomainTypes
        open Andromeda.Core

        let toProductInfo game : ProductInfo =
            { id = game.id
              title = game.name |> GameName.unwrap }
