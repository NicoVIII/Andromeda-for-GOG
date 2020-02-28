namespace Andromeda.Core.FSharp

open TypedPersistence.FSharp

module SettingsPersistence =
    let load() =
        loadDocumentFromDatabase<Settings> Database.name
        |> function
        | Ok appData ->
            appData
            |> Some
        | Error _ -> None

    let save = saveDocumentToDatabase<Settings> Database.name
