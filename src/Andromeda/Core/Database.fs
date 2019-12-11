namespace Andromeda.Core.FSharp

open LiteDB
open LiteDB.FSharp
open System.IO

open Andromeda.Core.FSharp

module Database =
    Directory.CreateDirectory(SystemInfo.savePath) |> ignore
    let database = Path.Combine(SystemInfo.savePath, "andromeda.db")
    let mapper = FSharpBsonMapper()

    let openDatabase () = new LiteDatabase(database, mapper)
