module Andromeda.AvaloniaApp.FSharp.Program

open Andromeda.AvaloniaApp.FSharp.Helpers
open Andromeda.AvaloniaApp.FSharp.ViewModels
open Andromeda.AvaloniaApp.FSharp.Windows

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.Installed
open Avalonia
open Avalonia.Controls
open Avalonia.Logging.Serilog
open GogApi.DotNet.FSharp.Base
open System.IO

let buildAvaloniaApp (args: string []): AppBuilder =
    let mutable builder = AppBuilder.Configure<App>().UsePlatformDetect().UseSkia().UseReactiveUI().LogToDebug()

    // Use CefGlue only on Windows for now...
    //builder <- builder.ConfigureCefGlue(args);
    builder

let createMainWindow appDataWrapper =
    let mainWindow = MainWindow()
    let mainWindowVM = MainWindowViewModel(mainWindow, appDataWrapper)
    mainWindowVM.Initialize()
    mainWindow.DataContext <- mainWindowVM
    match appDataWrapper.AppData.authentication with
    | NoAuth ->
        // Authenticate
        let window = AuthenticationWindow()
        window.DataContext <- AuthenticationWindowViewModel(window, mainWindowVM)
        window.ShowDialog mainWindow |> ignore
    | _ -> ()

    mainWindowVM.Init()
    mainWindow

let createSettingsWindow() =
    let settingsWindow = SettingsWindow()
    let settingsWindowVM = SettingsWindowViewModel(createMainWindow)
    settingsWindowVM.Initialize()
    settingsWindow.DataContext <- settingsWindowVM
    settingsWindow

let getStartWindow() =
    match AppDataPersistence.load() with
    | Some appData when appData.settings.gamePath |> Directory.Exists ->
        let appDataWrapper =
            appData
            |> searchInstalled AppDataPersistence.save
            |> AppDataWrapper
        createMainWindow appDataWrapper :> Window
    | Some _
    | None ->
        createSettingsWindow() :> Window

let start (app: Application) (_: string []) = app.Run(getStartWindow())

[<EntryPoint>]
let main (args: string []): int =
    buildAvaloniaApp(args).Start((fun app _ -> app.Run(getStartWindow())), [||])
    0
