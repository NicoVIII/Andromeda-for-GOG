namespace Andromeda.Core.FSharp

open GogApi.DotNet.FSharp.Base

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.PersistenceFunctions

module AppDataPersistence =
    module AndromedaDatabase = Andromeda.Core.FSharp.Database

    let private documentName = "appData"

    type AuthenticationPers = {
        accesscode: string;
        refreshcode: string;
    }

    type SettingsPers = {
        gamepath: string
    }

    type AppDataPers = {
        authentication: AuthenticationPers option
        installed: InstalledGame list
        gamepath: string
    }

    let private toPers (appData:AppData) =
        let authentication =
            match appData.authentication with
            | NoAuth ->
                None
            | Auth auth ->
                Some {
                    AuthenticationPers.refreshcode = auth.refreshToken
                    accesscode = auth.accessToken
                }
        {
            AppDataPers.authentication = authentication
            installed = appData.installedGames
            gamepath = appData.settings.gamePath
        }

    let private fromPers (appDataPers:AppDataPers) =
        let authentication =
            match appDataPers.authentication with
            | None ->
                NoAuth
            | Some auth ->
                Auth {
                    AuthenticationData.refreshToken = auth.refreshcode
                    accessToken = auth.accesscode
                    refreshed = false
                }
        {
            AppData.authentication = authentication
            installedGames = appDataPers.installed
            settings = {
                Settings.gamePath = appDataPers.gamepath
            }
        }

    let load () =
        let db = AndromedaDatabase.openDatabase ()
        loadDocumentWithMapping<AppDataPers, AppData> fromPers db documentName
        |> function
            | Ok appdata -> appdata
            | Error _ ->
                // TODO: log error
                AppData.createBasicAppData ()

    let save (appData: AppData) =
        let db = AndromedaDatabase.openDatabase ()
        saveDocumentWithMapping<AppDataPers, AppData> toPers db documentName appData
