namespace Andromeda.Core.FSharp.Persistence

open System.IO

open Andromeda.Core.FSharp

module Database =
    Directory.CreateDirectory(SystemInfo.savePath)
    |> ignore

    let name =
        Path.Combine(SystemInfo.savePath, Constants.databaseFile)