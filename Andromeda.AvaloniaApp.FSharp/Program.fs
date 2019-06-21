module Andromeda.AvaloniaApp.FSharp.Program

open Avalonia
open Avalonia.Logging.Serilog
open Couchbase.Lite
open Couchbase.Lite.Logging

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

    buildAvaloniaApp(args).Start<WebWindow>()
    
    0
