namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Threading
open Elmish
open GogApi.DotNet.FSharp.Listing

module InstallGame =
    type IInstallGameWindow =
        abstract Close: unit -> unit

        [<CLIEvent>]
        abstract OnSave: IEvent<IInstallGameWindow * ProductInfo>

        abstract Save: ProductInfo -> unit

    type State =
        { authentication: Authentication
          message: string
          search: string
          window: IInstallGameWindow }

    type Msg =
        | ChangeSearch of string
        | SearchGame
        | CloseWindow of ProductInfo option

    let init authentication (window: IInstallGameWindow) =
        { authentication = authentication
          message = ""
          search = ""
          window = window }, Cmd.none

    let update (msg: Msg) (state: State) =
        match msg with
        | ChangeSearch search ->
            { state with
                  search = search }, Cmd.none
        | SearchGame ->
            let invoke() =
                async {
                    let! (productList, _) = Authentication.withAutoRefresh
                                                (Games.getAvailableGamesForSearch state.search) state.authentication
                    match productList with
                    | Some [ product ] -> return Some product
                    | Some _
                    | None -> return None
                }
            { state with message = "" }, Cmd.OfAsync.perform invoke () CloseWindow
        | CloseWindow downloadStatus ->
            let message =
                match downloadStatus with
                | Some downloadStatus ->
                    downloadStatus |> state.window.Save
                    state.window.Close()
                    ""
                | None -> "No game/Too much games found."
            { state with message = message }, Cmd.none

    let view (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.margin 5.0
              StackPanel.orientation Orientation.Vertical
              StackPanel.spacing 5.0
              StackPanel.children
                  [ TextBox.create
                      [ TextBox.text state.search
                        TextBox.onTextChanged (ChangeSearch >> dispatch) ]
                    TextBlock.create [ TextBlock.text <| state.message ]
                    Button.create
                        [ Button.content "Search"
                          Button.onClick (fun _ -> SearchGame |> dispatch) ] ] ]

    type InstallGameWindow(authentication: Authentication) as this =
        inherit HostWindow()

        let saveEvent = new Event<_>()

        do
            base.Title <- "Install game"
            base.ShowInTaskbar <- false
            base.WindowStartupLocation <- WindowStartupLocation.CenterOwner
            base.Width <- 600.0
            base.Height <- 260.0

#if DEBUG
            this.AttachDevTools(KeyGesture(Key.F12))
#endif

            let syncDispatch (dispatch: Dispatch<'msg>): Dispatch<'msg> =
                match Dispatcher.UIThread.CheckAccess() with
                | true -> fun msg -> Dispatcher.UIThread.Post(fun () -> dispatch msg)
                | false -> dispatch

            Program.mkProgram (init authentication) update view
            |> Program.withHost this
            |> Program.withSyncDispatch syncDispatch
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWith (this)

        interface IInstallGameWindow with
            member __.Close() = this.Close()

            [<CLIEvent>]
            member __.OnSave = saveEvent.Publish
            // TODO: return authentication
            member __.Save(downloadInfo: ProductInfo) = saveEvent.Trigger(this :> IInstallGameWindow, downloadInfo)
