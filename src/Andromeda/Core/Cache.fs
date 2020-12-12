namespace Andromeda.Core

open System
open System.IO

open Andromeda.Core.DomainTypes
open Andromeda.Core.Helpers

/// A module for caching stuff. For now this holds methods concerning the cached installers
[<RequireQualifiedAccess>]
module Cache =
    /// This method performs an regular cache check. This is an operation which looks at the cached
    /// installers and decides if there is something to do about it
    let check settings =
        match settings.cacheRemoval with
        | RemoveByAge maxAge ->
            let path = SystemInfo.installerCachePath

            if Directory.Exists path then
                Directory.EnumerateFiles path
                // We are only interested in files, which are older than our deadline
                |> Seq.choose
                    (fun fileName ->
                        let filePath = Path.Combine(path, fileName)
                        let creationTime = File.GetCreationTime filePath

                        let deadline =
                            DateTime.Now.AddDays(maxAge |> float |> (*) -1.0)

                        match creationTime with
                        | creationTime when creationTime < deadline ->
                            Some filePath
                        | _ -> None)
                |> Seq.iter File.Delete
        | NoRemoval -> ()
