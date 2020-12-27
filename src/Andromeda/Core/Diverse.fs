namespace Andromeda.Core

open FsHttp.DslCE
open GogApi.DotNet.FSharp
open GogApi.DotNet.FSharp.DomainTypes
open System.Diagnostics.Contracts
open System.IO

open Andromeda.Core.Helpers

/// A module for everything, which does not really have a good place in structured modules
module Diverse =
    type GetProduct = unit -> Async<Result<GalaxyApi.ProductsResponse, string * string>>

    let getAvailableGamesForSearch name (authentication: Authentication) =
        async {
            let! result =
                Account.getFilteredGames
                    { feature = None
                      language = None
                      system = None
                      search = Some name
                      page = None
                      sort = None }
                    authentication

            return
                match result with
                | Ok response -> Some response.products
                | Error _ -> None
        }

    [<Pure>]
    /// Takes a list of Installers and returns only those, who are for the given OS
    let filterInstallersByOS systemInfo (installers: InstallerInfo list) =
        let filter =
            match systemInfo with
            | SystemInfo.OS.Linux -> fun (i: InstallerInfo) -> i.os = "linux"
            | SystemInfo.OS.Windows -> fun (i: InstallerInfo) -> i.os = "windows"
            | SystemInfo.OS.MacOS -> fun (i: InstallerInfo) -> i.os = "mac"

        List.filter filter installers

    let getAvailableInstallersForOs gameId authentication =
        async {
            let! result = GalaxyApi.getProduct gameId authentication

            return
                match result with
                | Ok response ->
                    filterInstallersByOS SystemInfo.os response.downloads.installers
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
