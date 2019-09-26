namespace Andromeda.Core.FSharp

open Couchbase.Lite
open System.IO

module Database =
    let private config = DatabaseConfiguration()
    Directory.CreateDirectory(SystemInfo.savePath) |> ignore
    config.Directory <- SystemInfo.savePath

    let get () = new Database("andromeda", config)
