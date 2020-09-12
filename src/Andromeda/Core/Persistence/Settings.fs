namespace Andromeda.Core.FSharp.Persistence

open TypedPersistence
open System.IO

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.DomainTypes

module Settings =
    let file =
        Path.Combine(SystemInfo.savePath, Constants.settingsFile)

    let load () = load<Settings> file

    let save = save<Settings> file
