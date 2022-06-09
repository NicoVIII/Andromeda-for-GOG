namespace Andromeda.Core

open SimpleOptics

[<RequireQualifiedAccess>]
module GameOptic =
    let id = Lens((fun (x: Game) -> x.id), (fun game value -> { game with id = value }))

    let name =
        Lens((fun (x: Game) -> x.name), (fun game value -> { game with name = value }))

    let image =
        Lens((fun (x: Game) -> x.image), (fun game value -> { game with image = value }))

    let status =
        Lens(
            (fun (x: Game) -> x.status),
            (fun game value -> { game with status = value })
        )
