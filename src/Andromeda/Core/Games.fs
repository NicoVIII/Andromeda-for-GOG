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

let getOwnedGameIds auth =
    askForOwnedGameIds auth
    |> exeFst (function
        | Some { owned = owned } -> owned
        | None -> []
    )

let startFileDownload url gameName version =
    let version =
        match version with
        | Some v -> "-v"
        | None -> ""
    let dir = Path.Combine(SystemInfo.cachePath, "installers")
    let filepath = Path.Combine(dir, sprintf "%s%s.%s" gameName version SystemInfo.installerEnding)
    let tmppath = Path.Combine(dir, "tmp", sprintf "%s%s.%s" gameName version SystemInfo.installerEnding)
    Directory.CreateDirectory(Path.Combine(dir, "tmp")) |> ignore
    let file = FileInfo(filepath)
    match not file.Exists with
    | true ->
        let url = String.replace "http://" "https://" url
        use client = new WebClient()
        let task = client.DownloadFileTaskAsync (url, tmppath)
        (task |> Some, filepath, tmppath)
    | false ->
        (None, filepath, tmppath)

let startGame path =
    match SystemInfo.os with
    | SystemInfo.OS.Linux
    | SystemInfo.OS.MacOS ->
        let filepath = Path.Combine(path, "start.sh")
        Syscall.chmod(filepath, FilePermissions.ALLPERMS) |> ignore
        Process.Start filepath |> ignore
    | SystemInfo.OS.Windows ->
        let file =
            Directory.GetFiles(path)
            |> List.ofArray
            |> List.filter (fun path ->
                let fileName = Path.GetFileName path
                fileName.StartsWith "Launch " && fileName.EndsWith ".lnk"
            )
            |> List.first
        match file with
        | Some file ->
            let target = getShortcutTarget file
            try
                Process.Start target |> ignore
            with
            | _ ->
                try
                    // Try again with admin rights
                    let p = new Process()
                    p.StartInfo.FileName <- target
                    p.StartInfo.UseShellExecute <- true
                    p.StartInfo.Verb <- "runas"
                    p.Start() |> ignore
                with
                | _ ->
                    ()
        | None ->
            () // TODO: better error handling

let rec copyDirectory (sourceDirName :string) (destDirName :string) (copySubDirs:bool) =
    let dir = DirectoryInfo(sourceDirName)
    let dirs = dir.GetDirectories();

    match (dir.Exists, Directory.Exists(destDirName)) with
    | (false, _) ->
        // If the source directory does not exist, throw an exception.
        raise (DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName))
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
    let random = Random();

    let rec helper rest result =
        let result = result + (string chars.[random.Next(chars.Length)]);
        match rest with
        | x when x > 1 -> helper (x-1) result
        | x -> result

    helper length ""

let extractLibrary (appData:AppData) (gamename: string) filepath =
    let target = Path.Combine(appData.settings.gamePath, gamename)
    printfn "%s" target
    match SystemInfo.os with
    | SystemInfo.OS.Linux ->
        Syscall.chmod (filepath, FilePermissions.ALLPERMS) |> ignore

        let tmp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "GOG Games", ".tmp", gamename)
        Directory.CreateDirectory(tmp) |> ignore
        try
            // Unzip linux installer with ZipLibrary
            let fastZip = FastZip()
            fastZip.ExtractZip (filepath, tmp, null)
        with
        | :? ZipException ->
            let p = Process.Start("unzip", "-qq \"" + filepath + "\" -d \""+tmp+"\"");
            p.WaitForExit()


        // Move files to install folder
        let folderPath = Path.Combine(tmp, "data", "noarch")
        Syscall.chmod (folderPath, FilePermissions.ALLPERMS) |> ignore
        let folder = DirectoryInfo(folderPath)
        match folder.Exists with
        | true ->
            copyDirectory folderPath target true
            Directory.Delete (tmp, true)
        | false ->
            failwith "Folder not found! :("
    | SystemInfo.OS.Windows ->
        let p = Process.Start(filepath, "/DIR=\"" + target+"\" /SILENT /VERYSILENT /SUPPRESSMSGBOXES /LANG=en /SP- /NOCANCEL /NORESTART")
        p.WaitForExit()
        match p.ExitCode with
        | 0 ->
            // Nothing to do
            ()
        | _ ->
            // Try again with non-silent install
            let p = Process.Start(filepath, "/DIR=\"" + target+"\" /SUPPRESSMSGBOXES /LANG=en /SP- /NOCANCEL /NORESTART")
            p.WaitForExit()
    | SystemInfo.OS.MacOS ->
        failwith "Not supported yet :/"

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
    askForProductInfo appData.authentication { ProductInfoRequest.id = gameId }
    |> function
        | (None, auth) ->
            ([], { appData with authentication = auth })
        | (Some response, auth) ->
            let installers = response.downloads.installers
            let installers' =
                installers
                |> fun info ->
                    match SystemInfo.os with
                    | SystemInfo.OS.Linux ->
                        List.filter (fun (i :InstallerInfo) -> i.os = "linux") info
                    | SystemInfo.OS.Windows ->
                        List.filter (fun (i :InstallerInfo) -> i.os = "windows") info
                    | SystemInfo.OS.MacOS ->
                        List.filter (fun (i :InstallerInfo) -> i.os = "mac") info
            (installers', { appData with authentication = auth })

let downloadGame (appData :AppData) gameName installer =
    installer.files
    |> function
        | (info::_) ->
            let (secUrl, auth) = askForSecureDownlink appData.authentication { downlink = info.downlink }
            let (task, filepath, tmppath) = startFileDownload secUrl.Value.downlink gameName installer.version
            Some (task, filepath, tmppath, info.size)
        | [] ->
            None
