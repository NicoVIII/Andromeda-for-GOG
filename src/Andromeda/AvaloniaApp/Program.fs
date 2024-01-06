namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Elmish
open GogApi

open System
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml.Styling

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
            |> Program.runWith authentication

    type AndromedaApplication() =
        inherit Application()

        override this.Initialize() =
            this.Styles.Add(
                StyleInclude(
                    baseUri = null,
                    Source = Uri("avares://Andromeda.AvaloniaApp/Styles.xaml")
                )
            )

            this.RequestedThemeVariant <- Styling.ThemeVariant.Dark

        override this.OnFrameworkInitializationCompleted() =
            match this.ApplicationLifetime with
            | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
                desktopLifetime.MainWindow <- MainWindow()
            | _ -> ()

    [<EntryPoint>]
    let main (args: string[]) : int =
        AppBuilder
            .Configure<AndromedaApplication>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
