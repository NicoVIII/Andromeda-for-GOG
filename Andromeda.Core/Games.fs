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
        let filepath =
            String.split '/' url
            |> List.last
            |> sprintf "%s%s" (Path.GetTempPath())
        use! resp =
            setupBasicRequest Get appData.authentication [] url
            |> getResponse
        use fileStream = new FileStream(filepath, FileMode.Create)
        do! resp.body.CopyToAsync fileStream |> Job.awaitUnitTask
        fileStream.Close ()

        match getOS () with
        | Linux | MacOS ->
            Syscall.chmod (filepath, FilePermissions.S_IXUSR) |> ignore
        | Windows | Unknown ->
            ()

        System.Diagnostics.Process.Start(filepath) |> ignore
    }

let installGame (appData :AppData) name =
    let (response, appData) = makeRequest<FilteredProductsResponse> Get appData [ createQuery "mediaType" "1"; createQuery "search" name ] "https://embed.gog.com/account/getFilteredProducts"
    printfn "%A" response
    match response with
    | Some { products = products } when products.Length = 1 ->
        let product = products.Head
        sprintf "https://embed.gog.com/account/gameDetails/%i.json" product.id
        |> makeRequest<GameDetailsResponse> Get appData [ createQuery "expand" "downloads" ]
        |> function
            | (None, appData) ->
                (false, appData)
            | (Some response, appData) ->
                let os =
                    match getOS () with
                    | Linux -> Some "linux"
                    | Windows -> Some "windows"
                    | MacOS -> Some "mac"
                    | Unknown -> None
                printfn "%A" response
                response.downloads
                |> List.head
                |> List.fold (fun lst info ->
                    match info with
                    | :? AllOSDownloadInfo as info ->
                        info::lst
                    | _ ->
                        lst
                ) []
                |> List.head
                |> (fun info ->
                    match getOS () with
                    | Linux -> Some info.linux
                    | Windows -> Some info.windows
                    | MacOS -> Some info.mac
                    | Unknown -> None
                )
                |> (function
                    | Some (info::_) ->
                        let url = sprintf "https://gog.com%s" info.manualUrl
                        downloadFile appData url |> run
                        (true, appData)
                    | Some [] | None ->
                        (false, appData)
                )
    | _ ->
        (false, appData)
