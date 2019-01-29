module Andromeda.Core.FSharp.Path

open System
open System.IO

open Andromeda.Core.FSharp.Helpers

let folderName = "andromeda"

let cachePath =
    let path =
        match os with
        | Linux
        | MacOS ->
            Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".cache", folderName)
        | Windows ->
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), folderName, "cache")
    Directory.CreateDirectory(path) |> ignore
    path

let tmpPath = Path.GetTempPath()

let gamePath =
    match os with
    | Linux
    | MacOS ->
        Environment.GetEnvironmentVariable "HOME"
        |> sprintf "%s/GOG Games"
    | Windows ->
        "d:\\Spiele" // TODO: tmp for debugging purposes

let savePath =
    match os with
    | Linux
    | MacOS ->
        Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".local/share/andromeda")
    | Windows ->
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), folderName, "save")

let installerEnding =
    match os with
    | Linux ->
        "sh"
    | MacOS ->
        "dmg"
    | Windows ->
        "exe"
