namespace Andromeda.Core.Persistence

open TypedPersistence.Core
open TypedPersistence.Json
open System.IO

open Andromeda.Core

module Settings =
    let file = Path.Combine(SystemInfo.savePath, Constants.settingsFile)

    let v1tov2 (v1: SettingsV1) : SettingsV2 =
        { cacheRemoval = NoRemoval
          gamePath = v1.gamePath }

    let v2tov3 (v2: SettingsV2) : Settings =
        { cacheRemoval = v2.cacheRemoval
          gamePath = v2.gamePath
          updateOnStartup = false }

    let load () =
        match getVersion file with
        | Some (Version version) ->
            match version with
            | 1u ->
                load<SettingsV1> file
                |> Option.map (v1tov2 >> v2tov3)
            | 2u -> load<SettingsV2> file |> Option.map v2tov3
            | _ -> load<Settings> file
        | None -> None

    let save = saveVersion<Settings> file (Version 3u)
