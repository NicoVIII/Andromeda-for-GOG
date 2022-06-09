namespace Andromeda.AvaloniaApp.Components

open Andromeda.Core
open Avalonia
open Avalonia.Controls

open Elmish
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout

open GogApi
open GogApi.DomainTypes

open Andromeda.AvaloniaApp

module InstallGame =
    type State =
        { dlcs: Dlc list option
          productInfos: ProductInfo list option
          search: string
          selected: ProductInfo option }

    type Intent =
        | DoNothing
        | Close of ProductInfo * Dlc list

    type Msg =
        | ChangeSearch of string
        | Close
        | SearchGame
        | SetProductInfos of ProductInfo list
        | SetDlcs of Dlc list option
        | SetSelected of ProductInfo

    let init () =
        { dlcs = None
          productInfos = None
          search = ""
          selected = None },
        Cmd.none

    let update authentication (msg: Msg) (state: State) =
        match msg with
        | ChangeSearch search -> { state with search = search }, Cmd.none, DoNothing
        | Close ->
            let intent =
                match state.selected with
                | Some selected ->
                    let dlcs = state.dlcs |> Option.defaultValue []
                    Intent.Close(selected, dlcs)
                | None -> failwith "No selected product"

            state, Cmd.none, intent
        | SearchGame ->
            let invoke () =
                async {
                    let! (productList, _) =
                        Helpers.withAutoRefresh
                            (Diverse.getAvailableGamesForSearch state.search)
                            authentication

                    return
                        match productList with
                        | Some products -> products
                        | None -> []
                }

            let state =
                { state with
                    productInfos = None
                    selected = None }

            let cmd = AvaloniaHelper.cmdOfAsync invoke () SetProductInfos

            state, cmd, DoNothing
        | SetProductInfos productInfos ->
            let cmd =
                // Preselect, if there is only one ProductInfo
                match productInfos with
                | [ productInfo ] -> Cmd.ofMsg <| SetSelected productInfo
                | _ -> Cmd.none

            let state = { state with productInfos = Some productInfos }

            state, cmd, DoNothing
        | SetDlcs dlcs -> { state with dlcs = dlcs }, Cmd.none, DoNothing
        | SetSelected productInfo ->
            let invoke () =
                async {
                    let! (gameInfo, _) =
                        Helpers.withAutoRefresh
                            (Account.getGameDetails productInfo.id)
                            authentication

                    return
                        match gameInfo with
                        | Ok gameInfo -> gameInfo.dlcs |> Some
                        | Error (x1, x2) -> failwithf "%s-%s" x1 x2
                }

            let state = { state with selected = Some productInfo }
            let cmd = AvaloniaHelper.cmdOfAsync invoke () SetDlcs
            state, cmd, DoNothing

    module View =
        let renderProductInfo (state: State) (dispatch: Msg -> unit) =
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

        let render installedGames (state: State) (dispatch: Msg -> unit) =
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
                    renderProductInfo state dispatch
                    TextBlock.create [
                        TextBlock.isVisible (
                            match state.selected with
                            | Some selected -> installedGames |> List.contains selected.id
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
                                installedGames |> List.contains selected.id |> not
                            | None -> false
                        )
                        Button.isVisible (
                            state.productInfos.IsSome
                            && state.productInfos.Value.Length > 0
                        )
                        Button.onClick (fun _ -> Close |> dispatch)
                    ]
                ]
            ]
