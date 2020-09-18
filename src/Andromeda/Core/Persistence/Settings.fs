namespace Andromeda.Core.FSharp.Persistence

open TypedPersistence
open System.IO

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.DomainTypes

module Settings =
    let file =
        Path.Combine(SystemInfo.savePath, Constants.settingsFile)

    let v1tov2 (v1: SettingsV1) =
        { Settings.cacheRemoval = NoRemoval
          Settings.gamePath = v1.gamePath }

    let load () =
        match getVersion file with
        | Some version ->
            match version with
            | 1u -> load<SettingsV1> file |> Option.map v1tov2
            | _ -> load<Settings> file
        | None -> None

    let save = save<Settings> file
