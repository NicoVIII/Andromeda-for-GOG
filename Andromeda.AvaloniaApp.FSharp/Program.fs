module Andromeda.AvaloniaApp.FSharp.Program

open Andromeda.Core.FSharp.AppData
open Andromeda.Core.FSharp.Installed
open Avalonia
open Avalonia.Logging.Serilog
open Couchbase.Lite
open Couchbase.Lite.Logging
open GogApi.DotNet.FSharp.Base

open Andromeda.AvaloniaApp.FSharp.Helpers
open Andromeda.AvaloniaApp.FSharp.ViewModels
open Andromeda.AvaloniaApp.FSharp.Windows

let buildAvaloniaApp (args: string[]): AppBuilder =
    let mutable builder =
        AppBuilder
         .Configure<App>()
         .UsePlatformDetect()
         .UseSkia()
         .UseReactiveUI()
         .LogToDebug()

    // Use CefGlue only on Windows for now...
    //builder <- builder.ConfigureCefGlue(args);
    builder

[<EntryPoint>]
let main (args: string[]): int =
    // Initialise Couchbase Lite
    Couchbase.Lite.Support.NetDesktop.Activate();
    Database.SetLogLevel(LogDomain.All, LogLevel.None);

    buildAvaloniaApp(args).Start(
        (fun app _ ->
            let appDataWrapper =
                loadAppData()
                |> searchInstalled
                |> AppDataWrapper

            let mainWindow = MainWindow ()
            let mainWindowVM = MainWindowViewModel (mainWindow, appDataWrapper)
            mainWindow.DataContext <- mainWindowVM
            match appDataWrapper.AppData.authentication with
            | NoAuth ->
                // Authenticate
                let window = AuthenticationWindow ()
                window.DataContext <- AuthenticationWindowViewModel (window, mainWindowVM)
                mainWindow |> window.ShowDialog |> ignore
            | _ -> ()
            mainWindowVM.Init()
            app.Run(mainWindow)
        ),
        [||]
    )

    0
