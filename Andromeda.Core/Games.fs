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
        printfn "%s" filepath
        printfn "%s" url

        use! resp =
            setupBasicRequest Head appData.authentication [] url
            |> Request.setHeader (UserAgent "Chrome")
            |> getResponse
        printfn "%A" (resp.headers.[ResponseHeader.Link])
        use fileStream = new FileStream(filepath, FileMode.Create)
        printfn "Start downloading..."
        resp.body.CopyTo fileStream
        printfn "Download completed!"
        fileStream.Close ()

        let fileinfo = new FileInfo(filepath)
        printfn "%i" fileinfo.Length

        match getOS () with
        | Linux | MacOS ->
            Syscall.chmod (filepath, FilePermissions.S_IXUSR) |> ignore
        | Windows | Unknown ->
            ()

        //System.Diagnostics.Process.Start(filepath) |> ignore
    }

let installGame (appData :AppData) name =
    let handleResponse (response :GameDetailsResponse) appData =
        response.downloads
        |> (function
            | lst when not (List.isEmpty lst) ->
                List.head lst
            | _ -> failwith "Nope: 1"
        )
        |> List.fold (fun lst info ->
            match info with
            | :? Map<string, obj> as info ->
                info::lst
            | _ -> lst
        ) []
        |> (function
            | lst when not (List.isEmpty lst) ->
                List.head lst
            | _ ->
                failwith "Nope: 2"
        )
        |> (fun info ->
            match getOS () with
            | Linux -> info.["linux"]
            | Windows -> info.["windows"]
            | MacOS -> info.["mac"]
        )
        |> function
            | :? (obj list) as info ->
                info
            | info ->
                printfn "%A" (info.GetType())
                failwith "Nope: 3"
        |> function
            | (info::_) ->
                match info with
                | :? Map<string, obj> as info ->
                    let url = sprintf "https://gog.com%s" ((string)info.["manualUrl"])
                    downloadFile appData url |> run
                | info ->
                    printfn "%A" (info.GetType())
                    failwith "Nope: 4"
                (true, appData)
            | [] ->
                (false, appData)

    let (response, appData) = makeRequest<FilteredProductsResponse> Get appData [ createQuery "mediaType" "1"; createQuery "search" name ] "https://embed.gog.com/account/getFilteredProducts"
    match response with
    | Some { products = products } when products.Length = 1 ->
        let product = products.Head
        // TODO: use other api to get working downlink, this one seems to need a cookie
        (*sprintf "https://embed.gog.com/account/gameDetails/%i.json" product.id
        |> makeRequest<GameDetailsResponse> Get appData [ createQuery "expand" "downloads" ]
        |> function
            | (None, appData) ->
                (false, appData)
            | (Some response, appData) ->
                handleResponse response appData*)
    | _ ->
        (false, appData)
