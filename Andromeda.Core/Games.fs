module Andromeda.Core.FSharp.Games

open Hopac
open HttpFs.Client
open Mono.Unix.Native
open System
open System.IO
open System.Net
open System.Threading.Tasks
open System.Timers

open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Helpers
open Andromeda.Core.FSharp.Responses

type GameId = GameId of int

let getOwnedGameIds (appData :AppData) =
    makeRequest<OwnedGamesResponse> Get appData [] "https://embed.gog.com/user/data/games"
    |> exeFst (function
        | Some { owned = owned } -> owned
        | None -> []
    )
    |> exeFst (List.map GameId)

let downloadFile (appData :AppData) url size =
    let size = float(size) / 1000000.0
    let filepath = Path.GetTempFileName ()
    let url = String.replace "http://" "https://" url

    use client = new WebClient()
    use task = client.DownloadFileTaskAsync (url, filepath)
    printf "Download started..."
    use timer = new System.Timers.Timer(1000.0)
    timer.AutoReset <- true
    timer.Elapsed.Add (fun _ ->
        let fileInfo = new FileInfo(filepath)
        float(fileInfo.Length) / 1000000.0
        |> printf "\rDownloading.. (%.1f MB of %.1f MB)    " <| size
    )
    timer.Start()
    task.Wait()
    timer.Stop()
    printfn "\rDownload completed!                   "

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

let getAvailableInstallersForOs (appData :AppData) product =
    sprintf "https://api.gog.com/products/%i" product.id
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
            downloadFile appData secUrl.Value.downlink info.size
            (true, appData)
        | [] ->
            (false, appData)
