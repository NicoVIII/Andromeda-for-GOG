namespace Andromeda.Core.FSharp

open GogApi.DotNet.FSharp.DomainTypes
open TypedPersistence.FSharp

module AuthenticationPersistence =
    let load() =
        loadDocumentFromDatabase<Authentication> Database.name
        |> function
        | Ok authentication ->
            authentication
            |> Some
        | Error _ -> None

    let save = saveDocumentToDatabase<Authentication> Database.name
