namespace Andromeda.Core.FSharp

open Andromeda.Core.FSharp.DomainTypes
open Andromeda.Core.FSharp.Helpers

open ICSharpCode.SharpZipLib.Zip
open FsHttp.DslCE
open GogApi.DotNet.FSharp
open GogApi.DotNet.FSharp.DomainTypes
open Mono.Unix.Native
open System
open System.Diagnostics
open System.IO
open System.Net

module Games =
    let getOwnedGameIds auth =
        async {
            let! result = User.getDataGames auth
            return match result with
                   | Ok { owned = owned } -> owned
                   | Error _ -> []
        }

    let startFileDownload (SafeDownLink url) gameName version =
        let version =
            match version with
            | Some v -> "-" + v
            | None -> ""

        // Remove invalid characters from gameName
        let gameName = Path.removeInvalidFileNameChars gameName

        let dir =
            Path.Combine(SystemInfo.cachePath, "installers")

        let filepath =
            Path.Combine
                (dir, sprintf "%s%s.%s" gameName version SystemInfo.installerEnding)

        let tmppath =
            Path.Combine
                (dir, "tmp", sprintf "%s%s.%s" gameName version SystemInfo.installerEnding)

        Directory.CreateDirectory(Path.Combine(dir, "tmp"))
        |> ignore
        let file = FileInfo(filepath)
        match not file.Exists with
        | true ->
            let url = url.Replace("http://", "https://")
            use client = new WebClient()

            let task =
                client.DownloadFileTaskAsync(url, tmppath)

            (task |> Some, filepath, tmppath)
        | false -> (None, filepath, tmppath)

    let private prepareGameProcess processOutput (proc: Process) =
        proc.StartInfo.RedirectStandardOutput <- true
        proc.OutputDataReceived.AddHandler(new DataReceivedEventHandler(processOutput))
        proc

    let private startWindowsGameProcess (prepareGameProcess: Process -> Process) path =
        let file =
            Directory.GetFiles(path)
            |> List.ofArray
            |> List.filter (fun path ->
                let fileName = Path.GetFileName path
                fileName.StartsWith "Launch "
                && fileName.EndsWith ".lnk")
            |> List.item 0

        let proc =
            new Process()
            |> fun proc ->
                proc.StartInfo.FileName <- Path.getShortcutTarget file
                proc
            |> prepareGameProcess

        try
            proc.Start() |> ignore
            proc.BeginOutputReadLine()
            proc |> Some
        with _ ->
            try
                // Try again with admin rights
                proc.StartInfo.UseShellExecute <- true
                proc.StartInfo.Verb <- "runas"
                proc.Start() |> ignore
                proc.BeginOutputReadLine()
                Some proc
            with _ -> None

    let startGameProcess processStandardOutput path =
        let prepareGameProcess = prepareGameProcess processStandardOutput
        match SystemInfo.os with
        | SystemInfo.OS.Linux
        | SystemInfo.OS.MacOS ->
            let filepath = Path.Combine(path, "start.sh")
            Syscall.chmod (filepath, FilePermissions.ALLPERMS)
            |> ignore

            let proc =
                new Process()
                |> fun proc ->
                    proc.StartInfo.FileName <- filepath
                    proc
                |> prepareGameProcess

            proc.Start() |> ignore
            proc.BeginOutputReadLine()
            proc |> Some
        | SystemInfo.OS.Windows -> startWindowsGameProcess prepareGameProcess path

    let rec copyDirectory
            (sourceDirName: string)
            (destDirName: string)
            (copySubDirs: bool)
        =
        let dir = DirectoryInfo(sourceDirName)
        let dirs = dir.GetDirectories()

        match (dir.Exists, Directory.Exists(destDirName)) with
        | (false, _) ->
            // If the source directory does not exist, throw an exception.
            raise
                (DirectoryNotFoundException
                    ("Source directory does not exist or could not be found: "
                     + sourceDirName))
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
            file.CopyTo(temppath, true) |> ignore)

        // If copySubDirs is true, copy the subdirectories.
        match copySubDirs with
        | true ->
            List.ofArray dirs
            |> List.iter (fun subdir ->
                // Create the subdirectory.
                let temppath = Path.Combine(destDirName, subdir.Name)

                // Copy the subdirectories.
                copyDirectory subdir.FullName temppath copySubDirs)
        | false -> ()

    let generateRandomString length =
        let chars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"

        let random = Random()

        let rec helper rest result =
            let result =
                result
                + (string chars.[random.Next(chars.Length)])

            match rest with
            | x when x > 1 -> helper (x - 1) result
            | x -> result

        helper length ""

    let createVersionFile gameDir version =
        File.WriteAllText(Path.Combine(gameDir, Constants.versionFile), version + "\n")

    let extractLibrary (settings: Settings) (gameName: string) filepath version =
        async {
            let gameName = Path.removeInvalidFileNameChars gameName

            let target =
                Path.Combine(settings.gamePath, gameName)

            match SystemInfo.os with
            | SystemInfo.OS.Linux ->
                Syscall.chmod (filepath, FilePermissions.ALLPERMS)
                |> ignore

                let tmp =
                    Path.Combine(settings.gamePath, ".tmp", gameName)
                // If there are some rests, remove them
                if Directory.Exists tmp then Directory.Delete(tmp, true) else ()
                Directory.CreateDirectory(tmp) |> ignore
                try
                    // Unzip linux installer with ZipLibrary
                    let fastZip = FastZip()
                    fastZip.ExtractZip(filepath, tmp, null)
                with :? ZipException ->
                    let p =
                        Process.Start
                            ("unzip", "-qq \"" + filepath + "\" -d \"" + tmp + "\"")

                    p.WaitForExit()

                // Move files to install folder
                let folderPath = Path.Combine(tmp, "data", "noarch")
                Syscall.chmod (folderPath, FilePermissions.ALLPERMS)
                |> ignore
                let folder = DirectoryInfo(folderPath)
                match folder.Exists with
                | true ->
                    copyDirectory folderPath target true
                    Directory.Delete(tmp, true)
                | false -> failwith "Folder not found! :("
            | SystemInfo.OS.Windows ->
                let p =
                    Process.Start
                        (filepath,
                         "/DIR=\""
                         + target
                         + "\" /SILENT /VERYSILENT /SUPPRESSMSGBOXES /LANG=en /SP- /NOCANCEL /NORESTART")

                p.WaitForExit()
                match p.ExitCode with
                | 0 ->
                    // Nothing to do
                    ()
                | _ ->
                    // Try again with non-silent install
                    let p =
                        Process.Start
                            (filepath,
                             "/DIR=\""
                             + target
                             + "\" /SUPPRESSMSGBOXES /LANG=en /SP- /NOCANCEL /NORESTART")

                    p.WaitForExit()
            | SystemInfo.OS.MacOS -> failwith "Not supported yet :/"

            match version with
            | Some version -> createVersionFile target version
            | None -> ()
        }

    let getAvailableGamesForSearch name (authentication: Authentication) =
        async {
            let! result =
                Account.getFilteredGames
                    { feature = None
                      language = None
                      system = None
                      search = Some name
                      page = None
                      sort = None } authentication
            return match result with
                   | Ok response -> Some response.products
                   | Error _ -> None
        }

    let getAvailableInstallersForOs gameId (authentication: Authentication) =
        async {
            let! result = GalaxyApi.getProduct gameId authentication
            return match result with
                   | Ok response ->
                       let installers = response.downloads.installers

                       let installers' =
                           installers
                           |> fun info ->
                               match SystemInfo.os with
                               | SystemInfo.OS.Linux ->
                                   List.filter (fun (i: InstallerInfo) -> i.os = "linux")
                                       info
                               | SystemInfo.OS.Windows ->
                                   List.filter (fun (i: InstallerInfo) -> i.os = "windows")
                                       info
                               | SystemInfo.OS.MacOS ->
                                   List.filter (fun (i: InstallerInfo) -> i.os = "mac")
                                       info

                       installers'
                   | Error _ -> []
        }

    let downloadGame gameName (installer: InstallerInfo) (authentication: Authentication) =
        async {
            match installer.files with
            | (info :: _) ->
                let! result = GalaxyApi.getSecureDownlink info.downlink authentication
                match result with
                | Ok urlResponse ->
                    let (task, filepath, tmppath) =
                        startFileDownload urlResponse.downlink gameName installer.version

                    return Some(task, filepath, tmppath, info.size)
                | Error _ ->
                    // TODO: Add loggin
                    return None
            | [] -> return None
        }

    let getProductImg productId authentication =
        let imgPath = SystemInfo.logo2xPath productId

        if File.Exists imgPath then
            async { return imgPath }
        else
            // Lade das Bild erstmal herunter
            async {
                let! response = GalaxyApi.getProduct productId authentication
                match response with
                | Ok productResponse ->
                    let imgUrl = "https:" + productResponse.images.logo2x

                    let imgResponse =
                        http {
                            GET imgUrl
                            CacheControl "no-cache"
                        }

                    let! imgData =
                        imgResponse.content.ReadAsByteArrayAsync()
                        |> Async.AwaitTask

                    imgPath
                    |> Path.GetDirectoryName
                    |> Directory.CreateDirectory
                    |> ignore
                    File.WriteAllBytes(imgPath, imgData)
                    return imgPath
                | Error _ -> return failwith "Fetching product info failed!"
            }
