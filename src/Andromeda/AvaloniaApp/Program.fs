namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Elmish
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open GogApi
open Avalonia.FuncUI

open Andromeda.AvaloniaApp

module Program =
    type MainWindow() as this =
        inherit HostWindow()

        do
            base.Title <- "Andromeda"

            base.Icon <-
                WindowIcon(
                    AvaloniaHelper.loadAssetPath
                        "avares://Andromeda.AvaloniaApp/Assets/logo.ico"
                )

            base.Width <- 1024.0
            base.Height <- 660.0

#if DEBUG
            this.AttachDevTools()
#endif

            // Try to load authentication from disk and refresh, if possible
            let authentication =
                Persistence.Authentication.load ()
                |> Option.bind (
                    Authentication.getRefreshToken
                    >> Async.RunSynchronously
                )
                // Save refreshed authentication
                |> Option.map (fun auth ->
                    Persistence.Authentication.save auth
                    auth)

            let updateWithServices msg state = Update.perform msg state this

            Program.mkProgram Init.perform updateWithServices View.render
            |> Program.withHost this
#if DEBUG
            |> Program.withTrace (fun msg _ -> printfn "%A" msg)
#endif
            |> Program.runWith authentication

    type AndromedaApplication() =
        inherit Application()

        override this.Initialize() =
            this.Styles.Load "avares://Avalonia.Themes.Default/DefaultTheme.xaml"
            this.Styles.Load "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml"
            this.Styles.Load "avares://Andromeda.AvaloniaApp/Styles.xaml"

        override this.OnFrameworkInitializationCompleted() =
            match this.ApplicationLifetime with
            | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
                desktopLifetime.MainWindow <- MainWindow()
            | _ -> ()

    [<EntryPoint>]
    let main (args: string []) : int =
        AppBuilder
            .Configure<AndromedaApplication>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
