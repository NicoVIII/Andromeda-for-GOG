namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Elmish
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Threading
open Elmish
open GogApi.DotNet.FSharp
open GogApi.DotNet.FSharp.DomainTypes

module InstallGame =
    type IInstallGameWindow =
        abstract Close: unit -> unit

        [<CLIEvent>]
        abstract OnSave: IEvent<IInstallGameWindow * ProductInfo>

        abstract Save: ProductInfo -> unit

    type State =
        { authentication: Authentication
          installedGames: ProductId list
          productInfos: ProductInfo list option
          search: string
          selected: ProductInfo option
          window: IInstallGameWindow }

    type Msg =
        | ChangeSearch of string
        | CloseWindow
        | SearchGame
        | SetProductInfos of ProductInfo list
        | SetSelected of ProductInfo

    let init authentication installedGames (window: IInstallGameWindow) =
        { authentication = authentication
          installedGames = installedGames
          productInfos = None
          search = ""
          selected = None
          window = window },
        Cmd.none

    let update (msg: Msg) (state: State) =
        match msg with
        | ChangeSearch search -> { state with search = search }, Cmd.none
        | CloseWindow ->
            match state.selected with
            | Some selected ->
                selected |> state.window.Save
                state.window.Close()
            | None -> ()
            state, Cmd.none
        | SearchGame ->
            let invoke () =
                async {
                    let! (productList, _) =
                        Helpers.withAutoRefresh
                            (Games.getAvailableGamesForSearch state.search)
                            state.authentication
                    return match productList with
                           | Some products -> products
                           | None -> []
                }

            { state with
                  productInfos = None
                  selected = None },
            Cmd.OfAsync.perform invoke () SetProductInfos
        | SetProductInfos productInfos ->
            let cmd =
                // Preselect, if there is only one ProductInfo
                match productInfos with
                | [ productInfo ] -> Cmd.ofMsg <| SetSelected productInfo
                | _ -> Cmd.none

            { state with
                  productInfos = Some productInfos },
            cmd
        | SetSelected productInfo ->
            { state with
                  selected = Some productInfo },
            Cmd.none

    let productInfoView (state: State) (dispatch: Msg -> unit) =
        let productInfoList =
            match state.productInfos with
            | Some productInfoList -> productInfoList
            | None -> []

        StackPanel.create
            [ StackPanel.children
                [ TextBlock.create
                    [ TextBlock.text "No games found!"
                      TextBlock.isVisible
                          (state.productInfos.IsSome
                           && state.productInfos.Value.Length = 0) ]
                  ListBox.create
                      [ ListBox.dataItems productInfoList
                        ListBox.itemTemplate
                            (DataTemplateView<ProductInfo>.create
                             <| fun productInfo ->
                                 TextBlock.create [ TextBlock.text productInfo.title ])
                        ListBox.isVisible
                            (state.productInfos.IsSome
                             && state.productInfos.Value.Length > 0)
                        match state.selected with
                        | Some selected -> ListBox.selectedItem selected
                        | None -> ()
                        ListBox.onSelectedItemChanged (fun obj ->
                            match obj with
                            | :? ProductInfo as p -> p |> SetSelected |> dispatch
                            | _ -> ()) ] ] ]

    let view (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.margin 5.0
              StackPanel.orientation Orientation.Vertical
              StackPanel.spacing 5.0
              StackPanel.children
                  [ DockPanel.create
                      [ DockPanel.margin (Thickness.Parse "0, 0, 0, 10")
                        DockPanel.children
                            [ Button.create
                                [ Button.content "Search"
                                  Button.dock Dock.Right
                                  Button.margin (Thickness.Parse "5, 0, 0, 0")
                                  Button.onClick (fun _ -> SearchGame |> dispatch) ]
                              TextBox.create
                                  [ TextBox.text state.search
                                    TextBox.onTextChanged (fun text ->
                                        match text = state.search with
                                        | true -> ()
                                        | false -> ChangeSearch text |> dispatch) ] ] ]
                    productInfoView state dispatch
                    TextBlock.create
                        [ TextBlock.isVisible
                            (match state.selected with
                             | Some selected ->
                                 state.installedGames |> List.contains selected.id
                             | None -> false)
                          TextBlock.text "This game is already installed" ]
                    Button.create
                        [ Button.content "Install"
                          // Nur aktivieren, wenn ein Game ausgewählt ist und dieses noch nicht installiert ist
                          Button.isEnabled
                              (match state.selected with
                               | Some selected ->
                                   state.installedGames
                                   |> List.contains selected.id
                                   |> not
                               | None -> false)
                          Button.isVisible
                              (state.productInfos.IsSome
                               && state.productInfos.Value.Length > 0)
                          Button.onClick (fun _ -> CloseWindow |> dispatch) ] ] ]

    type InstallGameWindow(authentication: Authentication, installedGames) as this =
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

            Program.mkProgram (init authentication installedGames) update view
            |> Program.withHost this
            |> Program.withSyncDispatch syncDispatch
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWith (this)

        interface IInstallGameWindow with
            member __.Close() = this.Close()

            [<CLIEvent>]
            override __.OnSave = saveEvent.Publish
            // TODO: return authentication
            member __.Save(downloadInfo: ProductInfo) =
                saveEvent.Trigger(this :> IInstallGameWindow, downloadInfo)
