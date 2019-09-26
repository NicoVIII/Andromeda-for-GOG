module Andromeda.Core.FSharp.SystemInfo

open Microsoft.FSharp.Collections
open System
open System.IO
open System.Runtime.InteropServices

type OS = Linux | MacOS | Windows

let os =
    // Map F# record to OSPlatform
    let mapPlatform platform =
        match platform with
        | Linux -> OSPlatform.Linux
        | MacOS -> OSPlatform.OSX
        | Windows -> OSPlatform.Windows

    // Wrap IsOSPlatform to use with OS record
    let isOS = mapPlatform >> RuntimeInformation.IsOSPlatform

    // Determine os
    [Linux; MacOS; Windows]
    |> List.tryFind isOS
    |> function
        | Some os -> os
        | None -> failwith "I couldn't determine your OS? :O" // TODO: Logger stuff?

let installerEnding =
    match os with
    | Linux ->
        "sh"
    | MacOS ->
        "dmg"
    | Windows ->
        "exe"

// TODO: Move to config
let cachePath =
    let path =
        match os with
        | Linux
        | MacOS ->
            Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".cache", Constants.folderName)
        | Windows ->
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.folderName, "cache")
    Directory.CreateDirectory(path) |> ignore
    path

// TODO: Move to config?
let tmpPath = Path.GetTempPath()

// TODO: Move to config
let gamePath =
    match os with
    | Linux
    | MacOS ->
        Environment.GetEnvironmentVariable "HOME"
        |> sprintf "%s/GOG Games"
    | Windows ->
        //"D:\\Spiele" // TODO: tmp for debugging purposes
        "C:\\Spiele"
        
// TODO: Move to config?
let savePath =
    match os with
    | Linux
    | MacOS ->
        Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".local/share/andromeda")
    | Windows ->
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.folderName, "save")
