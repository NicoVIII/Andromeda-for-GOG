module Andromeda.Core.FSharp.Path

open System
open System.IO

open Andromeda.Core.FSharp.Helpers

let folderName = "andromeda"

let cachePath =
    let path =
        match os with
        | Linux ->
            Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".cache", folderName)
        | MacOS ->
            failwith "Not supported yet :("
        | Windows ->
            failwith "Not supported yet :("
        | Unknown ->
            failwith "Something went wrong while determining on which system we are..."
    Directory.CreateDirectory(path) |> ignore
    path

let tmpPath = Path.GetTempPath()

let installerEnding =
    match os with
    | Linux ->
        "sh"
    | MacOS ->
        "dmg"
    | Windows ->
        "exe"
    | Unknown ->
        failwith "Something went wrong while determining on which system we are..."

