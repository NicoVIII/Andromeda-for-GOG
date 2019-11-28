namespace Andromeda.Core.FSharp

open Couchbase.Lite
open System.IO

open Andromeda.Core.FSharp

module Database =
    let private config = DatabaseConfiguration()
    Directory.CreateDirectory(SystemInfo.savePath) |> ignore
    config.Directory <- SystemInfo.savePath

    let openDatabase () = new Database("andromeda", config)
