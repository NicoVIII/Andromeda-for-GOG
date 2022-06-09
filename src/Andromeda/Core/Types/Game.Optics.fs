namespace Andromeda.Core

open SimpleOptics

[<RequireQualifiedAccess>]
module GameOptic =
    let id =
        Lens(
            (fun (x: Game) -> x.id),
            (fun (x: Game) (value: GogApi.DomainTypes.ProductId) -> { x with id = value })
        )

    let name =
        Lens(
            (fun (x: Game) -> x.name),
            (fun (x: Game) (value: string) -> { x with name = value })
        )

    let image =
        Lens(
            (fun (x: Game) -> x.image),
            (fun (x: Game) (value: string option) -> { x with image = value })
        )

    let status =
        Lens(
            (fun (x: Game) -> x.status),
            (fun (x: Game) (value: GameStatus) -> { x with status = value })
        )
