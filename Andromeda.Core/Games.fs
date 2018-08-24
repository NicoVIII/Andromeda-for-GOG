module Andromeda.Core.FSharp.Games

open HttpFs.Client
open Mono.Unix.Native
open System
open System.Diagnostics
open System.IO
open System.Net

open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Helpers
open Andromeda.Core.FSharp.Responses

let getOwnedGameIds (appData :AppData) =
    makeRequest<OwnedGamesResponse> Get appData [] "https://embed.gog.com/user/data/games"
    |> exeFst (function
        | Some { owned = owned } -> owned
        | None -> []
    )
    |> exeFst (List.map GameId)

let startFileDownload url =
    let filepath = Path.GetTempFileName ()
    let url = String.replace "http://" "https://" url

    use client = new WebClient()
    (client.DownloadFileTaskAsync (url, filepath), filepath)

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
        file.CopyTo(temppath, false) |> ignore
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
    //let gamename = gamename.Replace(" ", "\ ")
    match getOS () with
    | Linux ->
        Syscall.chmod (filepath, FilePermissions.S_IRWXU) |> ignore

        // Unzip linux installer
        let folderName = generateRandomString 20
        let tmp = Path.Combine(Path.GetTempPath(), folderName);
        let p = Process.Start("unzip", filepath+" -d \""+tmp+"\"");
        p.WaitForExit() |> ignore

        // Move files to install folder
        let folderPath = Path.Combine(tmp,"data","noarch")
        Syscall.chmod (folderPath, FilePermissions.ALLPERMS) |> ignore
        let folder = new DirectoryInfo(folderPath)
        match folder.Exists with
        | true ->
            let target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "GOG Games", gamename)
            copyDirectory folderPath target true
            Directory.Delete (tmp, true)
        | false ->
            failwith "Folder not found! :("
    | MacOS ->
        failwith "Not supported yet :/"
    | Windows ->
        failwith "Not supported yet :/"
    | Unknown ->
        failwith "Something strange happend! Couldn't recognise your os :O"

let getAvailableGamesForSearch (appData :AppData) name =
    let (response, appData) = makeRequest<FilteredProductsResponse> Get appData [ createQuery "mediaType" "1"; createQuery "search" name ] "https://embed.gog.com/account/getFilteredProducts"
    let products =
        match response with
        | None ->
            None
        | Some response ->
            Some response.products
    (products, appData)

let getAvailableInstallersForOs (appData :AppData) gameId =
    let (GameId id) = gameId
    sprintf "https://api.gog.com/products/%i" id
    |> makeRequest<ProductsResponse> Get appData [ createQuery "expand" "downloads" ]
    |> function
        | (None, appData) ->
            ([], appData)
        | (Some response, appData) ->
            let installers = response.downloads.installers
            let installers' =
                installers
                |> fun info ->
                    match getOS () with
                    | Linux ->
                        List.filter (fun (i :InstallerInfo) -> i.os = "linux") info
                    | Windows ->
                        List.filter (fun (i :InstallerInfo) -> i.os = "windows") info
                    | MacOS ->
                        List.filter (fun (i :InstallerInfo) -> i.os = "mac") info
                    | Unknown ->
                        []
            (installers', appData)

let downloadGame (appData :AppData) installer =
    installer.files
    |> function
        | (info::_) ->
            let url = info.downlink
            let secUrl = makeBasicJsonRequest<SecureUrlResponse> Get appData.authentication [] url
            let (task, filepath) = startFileDownload secUrl.Value.downlink
            Some (task, filepath, info.size)
        | [] ->
            None
