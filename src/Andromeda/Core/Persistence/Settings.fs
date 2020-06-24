namespace Andromeda.Core.FSharp.Persistence

open Andromeda.Core.FSharp.DomainTypes

open TypedPersistence.FSharp

module Settings =
    let load () =
        loadDocumentFromDatabase<Settings> Database.name
        |> function
        | Ok appData -> appData |> Some
        | Error _ -> None

    let save =
        saveDocumentToDatabase<Settings> Database.name
