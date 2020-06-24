module Andromeda.AvaloniaApp.FSharp.Program

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Load "avares://Avalonia.Themes.Default/DefaultTheme.xaml"
        this.Styles.Load "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml"
        this.Styles.Load "avares://Andromeda.AvaloniaApp.FSharp/Styles.xaml"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- Main.MainWindow()
        | _ -> ()

[<EntryPoint>]
let main (args: string []): int =
    AppBuilder.Configure<App>().UsePlatformDetect().UseSkia()
        .StartWithClassicDesktopLifetime(args)
