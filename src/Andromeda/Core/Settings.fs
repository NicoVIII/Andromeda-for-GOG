namespace Andromeda.Core.FSharp

open Couchbase.Lite

module Settings =
    module AndromedaDatabase = Andromeda.Core.FSharp.Database

    // TODO: remove and replace with typesave initialisation
    let tmpDefault = { Settings.gamePath = SystemInfo.gamePath }

    let save (settings:Settings) =
        use db = AndromedaDatabase.get ()
        use doc =
            match db.GetDocument "settings" with
            | null -> new MutableDocument("settings")
            | x -> x.ToMutable ()

        // Authentication
        let settingsDict = MutableDictionaryObject ()
        settingsDict.SetString ("gamepath", settings.gamePath) |> ignore

        db.Save doc
        db.Close ()

    let load () =
        use db = AndromedaDatabase.get ()
        use doc = db.GetDocument("settings")

        let appData =
            match doc with
            | null ->
                tmpDefault
            | doc ->
                let dict = doc.GetDictionary "settings"
                match dict with
                | null -> tmpDefault
                | dict ->
                    {
                        Settings.gamePath = dict.GetString "gamepath"
                    }
        db.Close ()
        appData
