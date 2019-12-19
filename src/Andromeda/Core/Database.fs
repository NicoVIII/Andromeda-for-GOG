namespace Andromeda.Core.FSharp

open LiteDB
open System.IO
open TypedPersistence.FSharp

open Andromeda.Core.FSharp

module Database =
    Directory.CreateDirectory(SystemInfo.savePath) |> ignore
    let database = Path.Combine(SystemInfo.savePath, "andromeda.db")
    let mapper = FSharpBsonMapperWithGenerics()

    let openDatabase () = new LiteDatabase(database, mapper)
