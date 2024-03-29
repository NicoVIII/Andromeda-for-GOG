namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Elmish
open GogApi

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish
open Avalonia.Themes.Simple

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
                |> Option.bind (Authentication.getRefreshToken >> Async.RunSynchronously)
                // Save refreshed authentication
                |> Option.map (fun auth ->
                    Persistence.Authentication.save auth
                    auth)

            let updateWithServices msg state = Update.perform msg state this

            Program.mkProgram Init.perform updateWithServices View.render
            |> Program.withHost this
#if DEBUG
            |> Program.withTrace (fun msg _ _ -> printfn "%A" msg)
#endif
            |> Program.runWithAvaloniaSyncDispatch authentication

    type AndromedaApplication() =
        inherit Application()

        override this.Initialize() =
            this.Styles.Add(new SimpleTheme())
            this.RequestedThemeVariant <- Styling.ThemeVariant.Dark
            this.Styles.Load "avares://Andromeda.AvaloniaApp/Styles.xaml"

        override this.OnFrameworkInitializationCompleted() =
            match this.ApplicationLifetime with
            | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
                desktopLifetime.MainWindow <- MainWindow()
            | _ -> ()

    [<EntryPoint>]
    let main args =
        AppBuilder
            .Configure<AndromedaApplication>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
