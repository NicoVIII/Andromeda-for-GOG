namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Elmish
open GogApi.DotNet.FSharp.DomainTypes
open System.Diagnostics
open System.Runtime.InteropServices
open System.Web

module Authentication =
    type IWindow =
        inherit IAndromedaWindow

        [<CLIEvent>]
        abstract OnSave: IEvent<IWindow * Authentication>

        abstract Save: Authentication -> unit

    let redirectUri = "https://embed.gog.com/on_login_success?origin=client"
    let authUri = "https://auth.gog.com/auth?client_id=46899977096215655&redirect_uri="
                             + (HttpUtility.UrlEncode redirectUri)
                             + "&response_type=code&layout=client2"

    type State =
        { authCode: string }

    type Msg =
        | OpenBrowser
        | CloseWindow of Authentication
        | Save
        | SetCode of string

    let init () =
        { authCode = "" }, Cmd.none

    let update (window: IWindow) (msg: Msg) (state: State) =
        match msg with
        | OpenBrowser ->
            // Open url in browser (from: https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/)
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                let command =
                    authUri.Replace("&", "^&")
                    |> sprintf "/c start %s"
                Process.Start(ProcessStartInfo("cmd", command)) |> ignore
            else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
                Process.Start("xdg-open", authUri) |> ignore
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) then
                Process.Start("open", authUri) |> ignore
            else
                ()
            state, Cmd.none
        | CloseWindow authentication ->
            authentication |> window.Save
            state, Cmd.none
        | Save ->
            let getAuth() =
                async {
                    let! authentication = Authentication.getNewToken redirectUri
                                              state.authCode
                    return authentication.Value }
            state, Cmd.OfAsync.perform getAuth () CloseWindow
        | SetCode code -> { state with authCode = code }, Cmd.none

    let view (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.margin 5.0
              StackPanel.orientation Orientation.Vertical
              StackPanel.spacing 5.0
              StackPanel.children
                  [ TextBlock.create [ TextBlock.text "Open this url in a browser and login:" ]
                    TextBox.create
                        [ TextBox.background "Transparent"
                          TextBox.borderThickness 0.0
                          TextBox.isReadOnly true
                          TextBox.textWrapping (TextWrapping.Wrap)
                          TextBox.text authUri ]
                    Button.create [
                        Button.content "Open in browser"
                        Button.onClick (fun _ -> OpenBrowser |> dispatch)
                    ]
                    TextBlock.create
                        [ TextBlock.text "Enter Code from url (..code=[code]) here:" ]
                    TextBox.create
                        [ TextBox.text state.authCode
                          TextBox.onTextChanged (SetCode >> dispatch) ]
                    Button.create
                        [ Button.content "Authenticate"
                          Button.isEnabled (state.authCode <> "")
                          Button.onClick (fun _ -> Save |> dispatch) ] ] ]

    type Window() as this =
        inherit AndromedaWindow()

        let saveEvent = new Event<_>()

        do
            base.Title <- "Authentication"
            base.ShowInTaskbar <- false
            base.WindowStartupLocation <- WindowStartupLocation.CenterOwner
            base.Width <- 600.0
            base.Height <- 260.0

#if DEBUG
            this.AttachDevTools(KeyGesture(Key.F12))
#endif

            let updateWithServices =
                update this

            Program.mkProgram init updateWithServices view
            |> Program.withHost this
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWith ()

        interface IWindow with
            [<CLIEvent>]
            member __.OnSave = saveEvent.Publish

            member __.Save(authentication: Authentication) =
                saveEvent.Trigger(this :> IWindow, authentication)
