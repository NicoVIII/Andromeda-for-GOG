namespace Andromeda.Core.DomainTypes

open SimpleOptics

[<AutoOpen>]
module Units =
    [<Measure>]
    type Byte

    [<Measure>]
    type MiB

    [<Measure>]
    type GiB

    let inline byteToMiB i = i / 1048576<Byte/MiB>
    let inline byteToGiB i = i / 1073741824<Byte/GiB>

    let inline byteToMiBL i = i / 1048576L<Byte/MiB>
    let inline byteToGiBL i = i / 1073741824L<Byte/GiB>

    let inline byteLToMiB (i: int64<Byte>) =
        let intMiB = byteToMiBL i |> int
        intMiB * 1<MiB>

type GameStatus =
    | Pending
    | Downloading of int<MiB> * int<MiB>
    | Installing
    | Installed of version: string

type Game =
    { id: GogApi.DomainTypes.ProductId
      name: string
      path: string
      updateable: bool
      image: string option
      status: GameStatus }

module Game =
    let create id name path =
        { Game.id = id
          name = name
          path = path
          updateable = false
          image = None
          status = Pending }

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

    let path =
        Lens(
            (fun (x: Game) -> x.path),
            (fun (x: Game) (value: string) -> { x with path = value })
        )

    let updateable =
        Lens(
            (fun (x: Game) -> x.updateable),
            (fun (x: Game) (value: bool) -> { x with updateable = value })
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
