namespace Andromeda.Core.FSharp.Persistence

open System.IO

open Andromeda.Core.FSharp

module SaveFiles =
    Directory.CreateDirectory(SystemInfo.savePath)
    |> ignore
