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

type DownloadInfo = {
    id: string;
    name: string;
    os: string;
    language: string;
    language_full: string;
    version: string;
    total_size: int;
}

type DownloadsInfo = {
    installers: DownloadInfo list
}

type GameInfoResponse = {
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
