namespace Andromeda.AvaloniaApp

open Andromeda.Core.Lenses
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Elmish
open GogApi.DotNet.FSharp
open GogApi.DotNet.FSharp.DomainTypes
open System.Diagnostics
open System.Runtime.InteropServices
open System.Web

module Authentication =
    let redirectUri =
        "https://embed.gog.com/on_login_success?origin=client"

    let authUri =
        "https://auth.gog.com/auth?client_id=46899977096215655&redirect_uri="
        + (HttpUtility.UrlEncode redirectUri)
        + "&response_type=code&layout=client2"

    type State = { authCode: string; invalidCode: bool }

    module StateLenses =
        let authCode =
            Lens((fun r -> r.authCode), (fun r v -> { r with authCode = v }))

    type Msg<'T> =
        | UseLens of Lens<State, 'T> * 'T
        | OpenBrowser
        | SetCode of string
        | TryAuthenticate of Authentication option
        | Save

    let init () = { authCode = ""; invalidCode = false }

    let update<'a> msg (state: State) toAuthMsg toGlobalMsg: State * Cmd<'a> =
        match msg with
        | UseLens (lens, value) ->
            let state = state |> setl lens value

            state, Cmd.none
        | OpenBrowser ->
            // Open url in browser (from: https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/)
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                let command =
                    authUri.Replace("&", "^&")
                    |> sprintf "/c start %s"

                Process.Start(ProcessStartInfo("cmd", command))
                |> ignore
            else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
                Process.Start("xdg-open", authUri) |> ignore
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) then
                Process.Start("open", authUri) |> ignore
            else
                ()
            state, Cmd.none
        | TryAuthenticate authentication ->
            match authentication with
            | Some authentication ->
                let cmd =
                    Global.Authenticate authentication
                    |> toGlobalMsg
                    |> Cmd.ofMsg

                state, cmd
            | None ->
                let state = { state with invalidCode = true }

                state, Cmd.none
        | Save ->
            let msg =
                match state.authCode with
                | "" -> Cmd.none
                | authCode ->
                    let getAuth () =
                        async { return! Authentication.getNewToken redirectUri authCode }

                    let msgFnc auth = TryAuthenticate auth |> toAuthMsg

                    Cmd.OfAsync.perform getAuth () msgFnc

            state, msg
        | SetCode code ->
            let state =
                { state with
                      authCode = code
                      invalidCode = false }

            state, Cmd.none

    let view (state: State) dispatch: IView =
        StackPanel.create
            [ StackPanel.margin 50.0
              StackPanel.orientation Orientation.Vertical
              StackPanel.spacing 5.0
              StackPanel.children
                  [ TextBlock.create
                      [ TextBlock.text "Open this url in a browser and login:" ]
                    TextBox.create
                        [ TextBox.background "Transparent"
                          TextBox.borderThickness 0.0
                          TextBox.isReadOnly true
                          TextBox.textWrapping (TextWrapping.Wrap)
                          TextBox.text authUri ]
                    Button.create
                        [ Button.content "Open in browser"
                          Button.onClick (fun _ -> OpenBrowser |> dispatch) ]
                    TextBlock.create
                        [ TextBlock.text "Enter Code from url (..code=[code]) here:" ]
                    TextBox.create
                        [ TextBox.text state.authCode
                          TextBox.onKeyDown (fun args ->
                            match args.Key with
                            | Key.Enter ->
                              Save |> dispatch
                            | _ -> ())
                          TextBox.onTextChanged (SetCode >> dispatch) ]
                    TextBlock.create
                        [ TextBlock.foreground "red"
                          TextBlock.text "Invalid code!"
                          TextBlock.isVisible state.invalidCode ]
                    Button.create
                        [ Button.content "Authenticate"
                          Button.isEnabled (state.authCode <> "")
                          Button.onClick (fun _ -> Save |> dispatch) ] ] ] :> IView