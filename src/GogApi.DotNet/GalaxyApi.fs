module GogApi.DotNet.FSharp.GalaxyApi

open HttpFs.Client

open GogApi.DotNet.FSharp.Base
open GogApi.DotNet.FSharp.Request

type ProductInfoRequest = {
    id: int;
}

type InstallerFileInfo = {
    id: string;
    size: int64;
    downlink: string;
}

type InstallerInfo = {
    id: string;
    os: string;
    version: string option;
    files: InstallerFileInfo list;
}

type DownloadsInfo = {
    installers: InstallerInfo list;
}

type ProductsResponse = {
    id: int;
    title: string;
    downloads: DownloadsInfo;
}

let askForProductInfo auth (request :ProductInfoRequest) =
    let queries = [
        createQuery "expand" "downloads"
    ]
    sprintf "https://api.gog.com/products/%i" request.id
    |> makeRequest<ProductsResponse> Get auth queries

type SecureUrlRequest = {
    downlink: string;
}

type SecureUrlResponse = {
    downlink: string;
    checksum: string;
}

let askForSecureDownlink auth (request :SecureUrlRequest) =
    makeRequest<SecureUrlResponse> Get auth [] request.downlink
