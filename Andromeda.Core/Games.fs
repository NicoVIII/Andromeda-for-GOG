module Andromeda.Core.FSharp.Games

open Hopac
open HttpFs.Client
open Mono.Unix.Native
open System.IO

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

let downloadFile (appData :AppData) url =
    job {
        let filepath = Path.GetTempFileName ()
        let url = String.replace "http://" "https://" url
        printfn "%s" filepath
        printfn "%s" url

        printfn "Start downloading..."
        use! resp =
            setupBasicRequest Get appData.authentication [] url
            |> getResponse
        use fileStream = new FileStream(filepath, FileMode.Create)
        printfn "Size of download: %s" resp.headers.[ResponseHeader.ContentLength]
        use task = resp.body.CopyToAsync fileStream
        do task.Wait()
        printfn "Download completed!"
        fileStream.Close ()

        let fileinfo = new FileInfo(filepath)
        printfn "%i" fileinfo.Length

        match getOS () with
        | Linux | MacOS ->
            Syscall.chmod (filepath, FilePermissions.S_IRWXU) |> ignore
        | Windows | Unknown ->
            ()

        let prog = System.Diagnostics.Process.Start(filepath)
        prog.WaitForExit() |> ignore
    }

let installGame (appData :AppData) name =
    let handleResponse (response :ProductsResponse) appData =
        let installers = response.downloads.installers
        printfn "%A" installers
        installers
        |> (fun info ->
            match getOS () with
            | Linux ->
                List.filter (fun (i :InstallerInfo) -> i.os = "linux") info
            | Windows ->
                List.filter (fun (i :InstallerInfo) -> i.os = "windows") info
            | MacOS ->
                List.filter (fun (i :InstallerInfo) -> i.os = "mac") info
            | Unknown ->
                []
        )
        |> (function
            | lst when not (List.isEmpty lst) ->
                let installer = List.head lst
                installer.files
            | _ -> failwith "Nope: 1"
        )
        |> function
            | (info::_) ->
                let url = info.downlink
                let secUrl = makeBasicJsonRequest<SecureUrlResponse> Get appData.authentication [] url
                downloadFile appData secUrl.Value.downlink
                |> run
                (true, appData)
            | [] ->
                (false, appData)

    let (response, appData) = makeRequest<FilteredProductsResponse> Get appData [ createQuery "mediaType" "1"; createQuery "search" name ] "https://embed.gog.com/account/getFilteredProducts"
    match response with
    | Some { products = products } when products.Length > 1 ->
        printfn "%A" products

        let product = products.Head
        sprintf "https://api.gog.com/products/%i" product.id
        |> makeRequest<ProductsResponse> Get appData [ createQuery "expand" "downloads" ]
        |> function
            | (None, appData) ->
                (false, appData)
            | (Some response, appData) ->
                handleResponse response appData
    | _ ->
        (false, appData)
