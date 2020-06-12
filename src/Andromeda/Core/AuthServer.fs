namespace Andromeda.Core.FSharp
open System
open System.Runtime.InteropServices
open System.Diagnostics

module AuthServer =
    open System.Net;
    open System.Threading;
    open System.Text

    type private AuthError =
        | InvalidCode
        | MissingCode

    let openUrl url =
        // FIXME: For some reason using this, removes most of the "url encoded"
        // parts of the URL, you can still manually copy paste the URL on the browser
        // but that's not ideal
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            let start = sprintf "/c start %s" url
            Process.Start(ProcessStartInfo("cmd", start)) |> ignore
        else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            Process.Start("xdg-open", url) |> ignore
        else if RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            Process.Start("open", url) |> ignore

    let private successResponse : array<byte> =
        let content: string =
            """
            <html>
            <head>Andromeda For GOG</head>
            <body>
                <p>Success</p>
                <p>You can close this window now.</p>
            </body>
            """
        Encoding.UTF8.GetBytes(content)

    let private errorResponse (error: AuthError) : array<byte> =
        let msg =
            match error with
            // may address github's #33?
            | InvalidCode -> "The Code provided in the Url is not valid, please try again."
            | MissingCode -> "We were unable to get the code from GOG, please try again."
        let content: string =
            sprintf """
            <html>
            <head>Andromeda For GOG</head>
            <body>
                <p>Error</p>
                <p>%s</p>
                <p>You can close this window now.</p>
            </body>
            """ msg
        Encoding.UTF8.GetBytes(content)


    let private validateToken (request: HttpListenerRequest): Result<string, AuthError> =
        /// TODO: ensure this code actually works (it should but can't test at the moment)
        let code = request.QueryString.Get "code" |> Option.ofObj
        match code with
        | Some code ->
            Ok code
        | None ->
            Error(MissingCode)

    type AuthServer(?port: int) =
        let httpListener = new HttpListener()
        let port =
            match port with
            | Some port -> port
            | None -> 9952
        let url = sprintf "http://127.0.0.1:%i/" port
        let validCode = new Event<string>()

        [<CLIEvent>]
        member __.OnValidCode = validCode.Publish

        [<DefaultValue(true)>]
        val mutable responseThread: Thread
        let mutable stopThread = false


        let threadResponse () =
            while not stopThread do
                let ctx = httpListener.GetContext()
                let response =
                    match validateToken(ctx.Request) with
                    | Ok code ->
                      validCode.Trigger code
                      successResponse
                    | Error err -> errorResponse err
                ctx.Response.OutputStream.Write(response, 0, response.Length)
                ctx.Response.KeepAlive <- false
                ctx.Response.Close()

        member __.Start() =
            httpListener.Prefixes.Add(url)
            printfn "Starting Auth server in  %s" url
            httpListener.Start()
            stopThread <- false
            __.responseThread <- Thread(ThreadStart(threadResponse))
            __.responseThread.Start()

        member __.Stop() =
            stopThread <- true
            httpListener.Stop()
