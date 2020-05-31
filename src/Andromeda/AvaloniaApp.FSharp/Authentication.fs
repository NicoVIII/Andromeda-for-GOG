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
open System.Web

module Authentication =
    type IAuthenticationWindow =
        abstract Close: unit -> unit

        [<CLIEvent>]
        abstract OnSave: IEvent<IAuthenticationWindow * Authentication>

        abstract Save: Authentication -> unit

    let redirectUri = "https://embed.gog.com/on_login_success?origin=client"

    type State =
        { authCode: string
          window: IAuthenticationWindow }

    type Msg =
        | CloseWindow of Authentication
        | Save
        | SetCode of string

    let init (window: IAuthenticationWindow) =
        { authCode = ""
          window = window }, Cmd.none

    let update (msg: Msg) (state: State) =
        match msg with
        | CloseWindow authentication ->
            authentication |> state.window.Save
            state.window.Close()
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
                  [ TextBlock.create [ TextBlock.text "Please go to" ]
                    TextBox.create
                        [ TextBox.background "Transparent"
                          TextBox.borderThickness 0.0
                          TextBox.isReadOnly true
                          TextBox.textWrapping (TextWrapping.Wrap)
                          TextBox.text
                          <| "https://auth.gog.com/auth?client_id=46899977096215655&redirect_uri="
                             + (HttpUtility.UrlEncode redirectUri)
                             + "&response_type=code&layout=client2" ]
                    TextBlock.create [ TextBlock.text "and log in." ]
                    TextBlock.create
                        [ TextBlock.text "Enter Code from url (..code=[code]) here:" ]
                    TextBox.create
                        [ TextBox.text state.authCode
                          TextBox.onTextChanged (SetCode >> dispatch) ]
                    Button.create
                        [ Button.content "Authenticate"
                          Button.isEnabled (state.authCode <> "")
                          Button.onClick (fun _ -> Save |> dispatch) ] ] ]

    type AuthenticationWindow(closeEventHandler) as this =
        inherit HostWindow()

        let saveEvent = new Event<_>()

        do
            base.Title <- "Authentication"
            base.ShowInTaskbar <- false
            base.WindowStartupLocation <- WindowStartupLocation.CenterOwner
            base.Width <- 600.0
            base.Height <- 260.0

            this.Closing.AddHandler closeEventHandler

#if DEBUG
            this.AttachDevTools(KeyGesture(Key.F12))
#endif

            Program.mkProgram init update view
            |> Program.withHost this
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWith (this)

        interface IAuthenticationWindow with
            member __.Close() =
                this.Closing.RemoveHandler closeEventHandler
                this.Close()

            [<CLIEvent>]
            member __.OnSave = saveEvent.Publish

            member __.Save(authentication: Authentication) =
                saveEvent.Trigger(this :> IAuthenticationWindow, authentication)
