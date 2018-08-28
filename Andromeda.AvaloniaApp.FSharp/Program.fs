module Andromeda.AvaloniaApp.FSharp.Program

open Andromeda.AvaloniaApp.FSharp.ViewModels
open Andromeda.AvaloniaApp.FSharp.Windows
open Avalonia
open Avalonia.Logging.Serilog
open CefGlue.Avalonia
open Couchbase.Lite
open Couchbase.Lite.Logging
open System

let buildAvaloniaApp (args: string[]) =
    let builder = AppBuilder.Configure<App>().UsePlatformDetect().UseSkia().UseReactiveUI().LogToDebug()

    // Use CefGlue only on Windows for now...
    //builder = builder.ConfigureCefGlue(args);
    builder

[<EntryPoint>]
let main (args: string[]) =
    // Initialise Couchbase Lite
    Couchbase.Lite.Support.NetDesktop.Activate();
    Database.SetLogLevel(LogDomain.All, LogLevel.None);

    buildAvaloniaApp(args).Start<MainWindow>(fun () -> new MainWindowViewModel() :> obj);
    0
