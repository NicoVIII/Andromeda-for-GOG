namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Avalonia
open Avalonia.Controls
open Avalonia.Diagnostics
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Elmish
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Threading
open Elmish
open GogApi
open GogApi.DomainTypes

module InstallGame =
    type IInstallGameWindow =
        abstract Close: unit -> unit

        [<CLIEvent>]
        abstract OnSave: IEvent<IInstallGameWindow * ProductInfo * Dlc list>

        abstract Save: ProductInfo * Dlc list -> unit

    type State =
        { authentication: Authentication
          installedGames: ProductId list
          dlcs: Dlc list option
          productInfos: ProductInfo list option
          search: string
          selected: ProductInfo option
          window: IInstallGameWindow }

    type Msg =
        | ChangeSearch of string
        | CloseWindow
        | SearchGame
        | SetProductInfos of ProductInfo list
        | SetDlcs of Dlc list option
        | SetSelected of ProductInfo

    let init authentication installedGames (window: IInstallGameWindow) =
        { authentication = authentication
          installedGames = installedGames
          dlcs = None
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
                let dlcs = state.dlcs |> Option.defaultValue []
                state.window.Save(selected, dlcs)
                state.window.Close()
            | None -> ()

            state, Cmd.none
        | SearchGame ->
            let invoke () =
                async {
                    let! (productList, _) =
                        Helpers.withAutoRefresh
                            (Diverse.getAvailableGamesForSearch state.search)
                            state.authentication

                    return
                        match productList with
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

            { state with productInfos = Some productInfos }, cmd
        | SetDlcs dlcs -> { state with dlcs = dlcs }, Cmd.none
        | SetSelected productInfo ->
            let invoke () =
                async {
                    let! (gameInfo, _) =
                        Helpers.withAutoRefresh
                            (Account.getGameDetails productInfo.id)
                            state.authentication

                    return
                        match gameInfo with
                        | Ok gameInfo -> gameInfo.dlcs |> Some
                        | Error (x1, x2) -> failwithf "%s-%s" x1 x2
                }

            { state with selected = Some productInfo },
            Cmd.OfAsync.perform invoke () SetDlcs

    let productInfoView (state: State) (dispatch: Msg -> unit) =
        let productInfoList =
            match state.productInfos with
            | Some productInfoList -> productInfoList
            | None -> []

        StackPanel.create [
            StackPanel.children [
                TextBlock.create [
                    TextBlock.text "No games found!"
                    TextBlock.isVisible (
                        state.productInfos.IsSome
                        && state.productInfos.Value.Length = 0
                    )
                ]
                ListBox.create [
                    ListBox.dataItems productInfoList
                    ListBox.itemTemplate (
                        DataTemplateView<ProductInfo>.create
                        <| fun productInfo ->
                            TextBlock.create [
                                TextBlock.text productInfo.title
                            ]
                    )
                    ListBox.isVisible (
                        state.productInfos.IsSome
                        && state.productInfos.Value.Length > 0
                    )
                    match state.selected with
                    | Some selected -> ListBox.selectedItem selected
                    | None -> ()
                    ListBox.onSelectedItemChanged (fun obj ->
                        match obj with
                        | :? ProductInfo as p -> p |> SetSelected |> dispatch
                        | _ -> ())
                ]
            ]
        ]

    let view (state: State) (dispatch: Msg -> unit) =
        StackPanel.create [
            StackPanel.margin 5.0
            StackPanel.orientation Orientation.Vertical
            StackPanel.spacing 5.0
            StackPanel.children [
                DockPanel.create [
                    DockPanel.margin (Thickness.Parse "0, 0, 0, 10")
                    DockPanel.children [
                        Button.create [
                            Button.content "Search"
                            Button.dock Dock.Right
                            Button.margin (Thickness.Parse "5, 0, 0, 0")
                            Button.onClick (fun _ -> SearchGame |> dispatch)
                        ]
                        TextBox.create [
                            TextBox.text state.search
                            TextBox.onKeyDown (fun args ->
                                match args.Key with
                                | Key.Enter -> SearchGame |> dispatch
                                | _ -> ())
                            TextBox.onTextChanged (fun text ->
                                match text = state.search with
                                | true -> ()
                                | false -> ChangeSearch text |> dispatch)
                        ]
                    ]
                ]
                productInfoView state dispatch
                TextBlock.create [
                    TextBlock.isVisible (
                        match state.selected with
                        | Some selected ->
                            state.installedGames |> List.contains selected.id
                        | None -> false
                    )
                    TextBlock.text "This game is already installed"
                ]
                match state.selected, state.dlcs with
                | None, _ -> ()
                | _, Some [] ->
                    TextBlock.create [
                        TextBlock.text "No DLCs found"
                    ]
                | _, Some dlcs ->
                    for dlc in dlcs do
                        TextBlock.create [
                            TextBlock.text dlc.title
                        ]

                    TextBlock.create [
                        TextBlock.text
                            "DLCs download and installation is not supported yet (WIP)"
                    ]
                | _, None ->
                    TextBlock.create [
                        TextBlock.text "Loading DLCs..."
                    ]
                Button.create [
                    Button.content "Install"
                    // Nur aktivieren, wenn ein Game ausgewählt ist und dieses noch nicht installiert ist
                    Button.isEnabled (
                        match state.selected with
                        | Some selected ->
                            state.installedGames
                            |> List.contains selected.id
                            |> not
                        | None -> false
                    )
                    Button.isVisible (
                        state.productInfos.IsSome
                        && state.productInfos.Value.Length > 0
                    )
                    Button.onClick (fun _ -> CloseWindow |> dispatch)
                ]
            ]
        ]

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
            DevTools.Attach(this, Config.devToolGesture)
            |> ignore
#endif

            let syncDispatch (dispatch: Dispatch<'msg>) : Dispatch<'msg> =
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
            member __.Save(downloadInfo: ProductInfo, dlcs: Dlc list) =
                saveEvent.Trigger(this :> IInstallGameWindow, downloadInfo, dlcs)
