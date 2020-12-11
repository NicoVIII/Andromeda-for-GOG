namespace Andromeda.Core.Persistence

open GogApi.DotNet.FSharp.DomainTypes
open TypedPersistence.Json
open System.IO

open Andromeda.Core

module Authentication =
    let file =
        Path.Combine(SystemInfo.savePath, Constants.authenticationFile)

    let load () = load<Authentication> file

    let save = save<Authentication> file
