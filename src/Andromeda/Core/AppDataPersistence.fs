namespace Andromeda.Core.FSharp

open GogApi.DotNet.FSharp.Base
open LiteDB
open LiteDB.FSharp.Extensions

open Andromeda.Core.FSharp

module AppDataPersistence =
    module AndromedaDatabase = Andromeda.Core.FSharp.Database

    type AuthenticationPers = {
        accesscode: string;
        refreshcode: string;
    }

    type SettingsPers = {
        id: int
        gamepath: string
    }

    type AppDataPers = {
        id: int
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
            id = 1
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
        let collection = db.GetCollection<AppDataPers>()
        match collection.TryFindById(BsonValue(1)) with
        | Some appDataPers ->
            appDataPers |> fromPers |> Some
        | None ->
            None

    let save (appData: AppData) =
        let db = AndromedaDatabase.openDatabase ()
        let appDataPers = appData |> toPers
        db.GetCollection<AppDataPers>().Upsert(appDataPers)
