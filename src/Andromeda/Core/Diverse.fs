namespace Andromeda.Core

open FsHttp.DslCE
open GogApi.DotNet.FSharp
open GogApi.DotNet.FSharp.DomainTypes
open System.IO

open Andromeda.Core.Helpers

/// A module for everything, which does not really have a good place in structured modules
module Diverse =
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

    type ProductImgResult =
        | AlreadyDownloaded of string
        | HasToBeDownloaded of (Authentication -> Async<ProductId * string>)

    let getProductImg productId =
        let imgPath = SystemInfo.logo2xPath productId

        if File.Exists imgPath then
            AlreadyDownloaded imgPath
        else
            (fun authentication ->
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
                        return productId, imgPath
                    | Error _ -> return failwith "Fetching product info failed!"
            })
            |> HasToBeDownloaded
