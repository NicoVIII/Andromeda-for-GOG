namespace Andromeda.Core.Persistence

open TypedPersistence.Core
open TypedPersistence.Json
open System.IO

open Andromeda.Core
open Andromeda.Core.DomainTypes

module Settings =
    let file =
        Path.Combine(SystemInfo.savePath, Constants.settingsFile)

    let v1tov2 (v1: SettingsV1) =
        { Settings.cacheRemoval = NoRemoval
          Settings.gamePath = v1.gamePath }

    let load () =
        match getVersion file with
        | Some (Version version) ->
            match version with
            | 1u -> load<SettingsV1> file |> Option.map v1tov2
            | _ -> load<Settings> file
        | None -> None

    let save = saveVersion<Settings> file (Version 2u)
