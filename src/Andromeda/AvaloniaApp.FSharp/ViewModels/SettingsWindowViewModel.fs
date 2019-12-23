namespace Andromeda.AvaloniaApp.FSharp.ViewModels

open Andromeda.AvaloniaApp.FSharp.Helpers
open Andromeda.AvaloniaApp.FSharp.Windows

open Andromeda.Core.FSharp
open Avalonia.Controls
open ReactiveUI
open System.IO
open System.Reactive
open System.Reactive.Linq

type SettingsWindowViewModel(appDataWrapper: AppDataWrapper option, createMainWindow: AppDataWrapper -> MainWindow) as this =
    inherit ReactiveObject()

    let mutable gamePath =
        match appDataWrapper with
        | Some appDataWrapper ->
            appDataWrapper.AppData.settings.gamePath
        | None ->
            ""
    let mutable invalid: ObservableAsPropertyHelper<bool> = null

    new(createMainWindow) = SettingsWindowViewModel(None, createMainWindow)
    new(appDataWrapper: AppDataWrapper) = SettingsWindowViewModel(Some appDataWrapper, fun _ -> failwith "Should not be called...")

    member this.GamePath
        with private get () = gamePath
        and private set value =
            this.RaiseAndSetIfChanged(&gamePath, value) |> ignore
    member __.Invalid = invalid.Value

    member val SaveCommand: ReactiveCommand<Window, Unit> = ReactiveCommand.Create<Window>(this.Save)

    // Necessary, because F# wants to initialize EVERYTHING before using ANYTHING...
    member __.Initialize() =
        invalid <-
            this.WhenAnyValue<SettingsWindowViewModel, string>(fun (x: SettingsWindowViewModel) -> x.GamePath)
                .Select(Directory.Exists >> not)
                .ToProperty(this, (fun (x: SettingsWindowViewModel) -> x.Invalid))

    member this.Save(window: Window) =
        if this.GamePath |> Directory.Exists then
            match appDataWrapper with
            | Some appDataWrapper ->
                if appDataWrapper.AppData.settings.gamePath <> this.GamePath then
                    let appData = { appDataWrapper.AppData with settings = { Settings.gamePath = this.GamePath } }
                    appDataWrapper.AppData <- Installed.searchInstalled appData
                else
                    ()
            // Initial call to set initial settings
            | None ->
                let mainWindow =
                    AppData.createBasicAppData { Settings.gamePath = this.GamePath }
                    |> AppDataWrapper
                    |> createMainWindow
                mainWindow.Show ()
            window.Close()
        else
            failwith "Saving should not be allowed, if path does not exist!"
