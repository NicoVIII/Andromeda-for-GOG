namespace Andromeda.Core

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GameName =
    let create value = GameName value
    let unwrap (GameName name) = name

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GamePath =
    open Andromeda.Core.Helpers

    let private create value = GamePath value

    let ofName name =
        name
        |> GameName.unwrap
        |> Path.removeInvalidFileNameChars
        |> create

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Game =
    let create id name =
        { Game.id = id
          name = GameName.create name
          image = None
          status = Pending }
