namespace Andromeda.Core

open ICSharpCode.SharpZipLib.Zip
open FsHttp
open FsHttp.DslCE
open GogApi
open GogApi.DomainTypes
open Mono.Unix.Native
open System.Diagnostics
open System.IO
open System.Net.Http
open System.Text.RegularExpressions

open Andromeda.Core.DomainTypes
open Andromeda.Core.Helpers

/// A module for everything which helps to download and install games
module Download =
    let client = new HttpClient()

    let startFileDownload (SafeDownLink url) gameName version =
        let version =
            match version with
            | Some v -> "-" + v
            | None -> ""

        // Remove invalid characters from gameName
        let gameName = Path.removeInvalidFileNameChars gameName

        let dir = SystemInfo.installerCachePath

        let filepath =
            Path.Combine(
                dir,
                sprintf "%s%s.%s" gameName version SystemInfo.installerEnding
            )

        let tmppath =
            Path.Combine(
                dir,
                "tmp",
                sprintf "%s%s.%s" gameName version SystemInfo.installerEnding
            )

        Directory.CreateDirectory(Path.Combine(dir, "tmp"))
        |> ignore

        let file = FileInfo(filepath)

        match file.Exists with
        | false ->
            let url = url.Replace("http://", "https://")

            let task =
                task {
                    use fileStream = File.Create tmppath

                    let! response =
                        client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)

                    do response.EnsureSuccessStatusCode() |> ignore
                    do! response.Content.CopyToAsync fileStream
                }

            (task |> Some, filepath, tmppath)
        | true -> (None, filepath, tmppath)

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
            raise (
                DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName
                )
            )
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

    let createVersionFile gameDir version =
        File.WriteAllText(Path.Combine(gameDir, Constants.versionFile), version + "\n")

    let extractLibrary (settings: Settings) (gameName: string) filepath version =
        async {
            let gameName = Path.removeInvalidFileNameChars gameName

            let target = Path.Combine(settings.gamePath, gameName)

            match SystemInfo.os with
            | SystemInfo.OS.Linux ->
                Syscall.chmod (filepath, FilePermissions.ALLPERMS)
                |> ignore

                let tmp = Path.Combine(settings.gamePath, ".tmp", gameName)
                // If there are some rests, remove them
                if Directory.Exists tmp then
                    Directory.Delete(tmp, true)
                else
                    ()

                Directory.CreateDirectory(tmp) |> ignore

                try
                    // Unzip linux installer with ZipLibrary
                    let fastZip = FastZip()
                    fastZip.ExtractZip(filepath, tmp, null)
                with
                | :? ZipException ->
                    let p =
                        Process.Start(
                            "unzip",
                            "-qq \"" + filepath + "\" -d \"" + tmp + "\""
                        )

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
                    Process.Start(
                        filepath,
                        "/DIR=\""
                        + target
                        + "\" /SILENT /VERYSILENT /SUPPRESSMSGBOXES /LANG=en /SP- /NOCANCEL /NORESTART"
                    )

                p.WaitForExit()

                match p.ExitCode with
                | 0 ->
                    // Nothing to do
                    ()
                | _ ->
                    // Try again with non-silent install
                    let p =
                        Process.Start(
                            filepath,
                            "/DIR=\""
                            + target
                            + "\" /SUPPRESSMSGBOXES /LANG=en /SP- /NOCANCEL /NORESTART"
                        )

                    p.WaitForExit()
            | SystemInfo.OS.MacOS -> failwith "Not supported yet :/"

            match version with
            | Some version -> createVersionFile target version
            | None -> ()

            return target
        }

    let downloadGame
        gameName
        (installer: InstallerInfo)
        (authentication: Authentication)
        =
        async {
            match installer.files with
            | (info :: _) ->
                let! result = GalaxyApi.getSecureDownlink info.downlink authentication

                match result with
                | Ok urlResponse ->
                    return!
                        async {
                            // Get checksum
                            let! response = httpAsync { GET urlResponse.checksum }

                            let checksum =
                                Regex
                                    .Match(
                                        Response.toText response,
                                        "md5=\"([a-z0-9]+)\""
                                    )
                                    .Groups[1]
                                    .Value

                            let (task, filepath, tmppath) =
                                startFileDownload
                                    urlResponse.downlink
                                    gameName
                                    installer.version

                            return
                                Some(
                                    task,
                                    filepath,
                                    tmppath,
                                    info.size * 1L<Byte>,
                                    checksum
                                )
                        }
                | Error _ ->
                    // TODO: Add loggin
                    return None
            | [] -> return None
        }
