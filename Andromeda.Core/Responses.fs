module Andromeda.Core.FSharp.Responses

type TokenResponse = {
    expires_in: int;
    scope: string;
    token_type: string;
    access_token: string;
    user_id: string;
    refresh_token: string;
    session_id: string;
}

type OwnedGamesResponse = {
    owned: int list;
}

type UserDataResponse = {
    username: string;
    email: string;
}

type FileResponseInfo = {
    id: string;
    size: int;
    downlink: string;
}

type DownloadInfo = {
    manualUrl: string;
}

type AllOSDownloadInfo = {
    windows: DownloadInfo list
    mac: DownloadInfo list
    linux: DownloadInfo list
}

type GameDetailsResponse = {
    title: string;
    downloads: obj list list;
}

type InstallerFileInfo = {
    id: string;
    size: int64;
    downlink: string;
}

type InstallerInfo = {
    id: string;
    os: string;
    version: string;
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

type ProductInfo = {
    id: int;
    title: string;
}

type FilteredProductsResponse = {
    totalProducts: int;
    products: ProductInfo list;
}

type SecureUrlResponse = {
    downlink: string;
    checksum: string;
}
