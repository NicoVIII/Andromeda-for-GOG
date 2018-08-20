module Andromeda.Core.FSharp.Games

open HttpFs.Client
open Mono.Unix.Native
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

let executeFile filepath =
    match getOS () with
    | Linux | MacOS ->
        Syscall.chmod (filepath, FilePermissions.S_IRWXU) |> ignore
    | Windows | Unknown ->
        ()

    let prog = System.Diagnostics.Process.Start(filepath)
    prog.WaitForExit() |> ignore

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
