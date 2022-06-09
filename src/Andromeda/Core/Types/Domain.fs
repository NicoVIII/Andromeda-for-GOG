namespace Andromeda.Core

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
    | Errored of msg: string
    | Downloading of int<MiB> * int<MiB> * filepath: string
    | Installing of filepath: string
    | Installed of version: string option * gameDir: string

type GameName = private GameName of string
type GamePath = private GamePath of string

type Game =
    { id: GogApi.DomainTypes.ProductId
      name: GameName
      image: string option
      status: GameStatus }
