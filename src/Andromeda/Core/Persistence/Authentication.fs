namespace Andromeda.Core.FSharp.Persistence

open GogApi.DotNet.FSharp.DomainTypes
open TypedPersistence.FSharp

module Authentication =
    let load () =
        loadDocumentFromDatabase<Authentication> Database.name
        |> function
        | Ok authentication -> authentication |> Some
        | Error _ -> None

    let save =
        saveDocumentToDatabase<Authentication> Database.name
