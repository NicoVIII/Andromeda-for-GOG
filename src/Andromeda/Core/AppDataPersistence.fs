namespace Andromeda.Core.FSharp

open GogApi.DotNet.FSharp.Base
open TypedPersistence.FSharp

open Andromeda.Core.FSharp

module AppDataPersistence =
    type AuthenticationPers =
        { accesscode: string
          refreshcode: string }

    type AppDataPers =
        { authentication: AuthenticationPers option
          gamepath: string }

    let private toPers (appData: AppData) =
        let authentication =
            match appData.authentication with
            | NoAuth -> None
            | Auth auth ->
                Some
                    { AuthenticationPers.refreshcode = auth.refreshToken
                      accesscode = auth.accessToken }
        { AppDataPers.authentication = authentication
          gamepath = appData.settings.gamePath }

    let private fromPers (appDataPers: AppDataPers) =
        let authentication =
            match appDataPers.authentication with
            | None -> NoAuth
            | Some auth ->
                Auth
                    { AuthenticationData.refreshToken = auth.refreshcode
                      accessToken = auth.accesscode
                      refreshed = false }
        { AppData.authentication = authentication
          installedGames = []
          settings = { Settings.gamePath = appDataPers.gamepath } }

    let load() =
        loadDocumentFromDatabase<AppDataPers> Database.name
        |> function
        | Ok appData ->
            appData
            |> fromPers
            |> Some
        | Error error ->
            None

    let save (appData: AppData) =
        appData
        |> toPers
        |> saveDocumentToDatabase<AppDataPers> Database.name
