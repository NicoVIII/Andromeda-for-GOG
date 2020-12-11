namespace Andromeda.Core.Persistence

open System.IO

open Andromeda.Core

module SaveFiles =
    Directory.CreateDirectory(SystemInfo.savePath)
    |> ignore
