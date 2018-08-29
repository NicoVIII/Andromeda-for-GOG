module Andromeda.Core.FSharp.Games

open ICSharpCode.SharpZipLib.Zip
open GogApi.DotNet.FSharp.GamesMovies
open GogApi.DotNet.FSharp.Listing
open GogApi.DotNet.FSharp.GalaxyApi
open Mono.Unix.Native
open System
open System.Diagnostics
open System.IO
open System.Net

open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Helpers
open Andromeda.Core.FSharp.Path

let getOwnedGameIds auth =
    askForOwnedGameIds auth
    |> exeFst (function
        | Some { owned = owned } -> owned
        | None -> []
    )
    |> exeFst (List.map GameId)

let startFileDownload url gameName version =
    let dir = Path.Combine(cachePath, "installers")
    let filepath = Path.Combine(dir, sprintf "%s-%s.%s" gameName version installerEnding)
    Directory.CreateDirectory(dir) |> ignore
    let file = new FileInfo(filepath)
    match not file.Exists with
    | true ->
        let url = String.replace "http://" "https://" url
        use client = new WebClient()
        (client.DownloadFileTaskAsync (url, filepath) |> Some, filepath)
    | false ->
        (None, filepath)

let rec copyDirectory (sourceDirName :string) (destDirName :string) (copySubDirs:bool) =
    let dir = new DirectoryInfo(sourceDirName)
    let dirs = dir.GetDirectories();

    match (dir.Exists, Directory.Exists(destDirName)) with
    | (false, _) ->
        // If the source directory does not exist, throw an exception.
        raise (new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName))
    | (true, false) ->
        // If the destination directory does not exist, create it.
        Directory.CreateDirectory(destDirName) |> ignore
    | (true, true) -> ()

    // Get the file contents of the directory to copy.
    dir.GetFiles()
    |> List.ofArray
    |> List.iter (fun file ->
        // Create the path to the new copy of the file.
        let temppath = Path.Combine(destDirName, file.Name)

        // Copy the file.
        file.CopyTo(temppath, true) |> ignore
    )

    // If copySubDirs is true, copy the subdirectories.
    match copySubDirs with
    | true ->
        List.ofArray dirs
        |> List.iter (fun subdir ->
            // Create the subdirectory.
            let temppath = Path.Combine(destDirName, subdir.Name)

            // Copy the subdirectories.
            copyDirectory subdir.FullName temppath copySubDirs
        )
    | false -> ()

let generateRandomString length =
    let chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    let random = new Random();

    let rec helper rest result =
        let result = result + (string chars.[random.Next(chars.Length)]);
        match rest with
        | x when x > 1 -> helper (x-1) result
        | x -> result

    helper length ""

let extractLibrary (gamename: string) filepath =
    let target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "GOG Games", gamename)
    match os with
    | Linux ->
        Syscall.chmod (filepath, FilePermissions.ALLPERMS) |> ignore

        // Unzip linux installer
        let folderName = generateRandomString 20
        let tmp = Path.Combine(Path.GetTempPath(), folderName)
        let fastZip = new FastZip()
        fastZip.ExtractZip (filepath, tmp, null)

        // Move files to install folder
        let folderPath = Path.Combine(tmp,"data","noarch")
        Syscall.chmod (folderPath, FilePermissions.ALLPERMS) |> ignore
        let folder = new DirectoryInfo(folderPath)
        match folder.Exists with
        | true ->
            copyDirectory folderPath target true
            Directory.Delete (tmp, true)
        | false ->
            failwith "Folder not found! :("
    | Windows ->
        let p = Process.Start(filepath, "/SILENT /LANG=en /SP- /NOCANCEL /SUPPRESSMSGBOXES /NOGUI /DIR=\"" + target+"\"")
        p.WaitForExit() |> ignore
    | MacOS ->
        failwith "Not supported yet :/"
    | Unknown ->
        failwith "Something strange happend! Couldn't recognise your os :O"

let getAvailableGamesForSearch (appData :AppData) name =
    let (response, auth) = askForFilteredProducts appData.authentication { search = name }
    let products =
        match response with
        | None ->
            None
        | Some response ->
            Some response.products
    (products, { appData with authentication = auth })

let getAvailableInstallersForOs (appData :AppData) gameId =
    let (GameId id) = gameId
    askForProductInfo appData.authentication { ProductInfoRequest.id = id }
    |> function
        | (None, auth) ->
            ([], { appData with authentication = auth })
        | (Some response, auth) ->
            let installers = response.downloads.installers
            let installers' =
                installers
                |> fun info ->
                    match os with
                    | Linux ->
                        List.filter (fun (i :InstallerInfo) -> i.os = "linux") info
                    | Windows ->
                        List.filter (fun (i :InstallerInfo) -> i.os = "windows") info
                    | MacOS ->
                        List.filter (fun (i :InstallerInfo) -> i.os = "mac") info
                    | Unknown ->
                        []
            (installers', { appData with authentication = auth })

let downloadGame (appData :AppData) gameName installer =
    installer.files
    |> function
        | (info::_) ->
            let (secUrl, auth) = askForSecureDownlink appData.authentication { downlink = info.downlink }
            let (task, filepath) = startFileDownload secUrl.Value.downlink gameName installer.version
            Some (task, filepath, info.size)
        | [] ->
            None
