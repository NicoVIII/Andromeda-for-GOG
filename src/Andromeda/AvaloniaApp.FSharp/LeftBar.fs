namespace Andromeda.AvaloniaApp.FSharp

open DomainTypes

open Andromeda.Core.FSharp
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout
open Avalonia.Media
open Elmish

module LeftBar =
    type State = { searchString: string }

    type Msg = Search of string

    let init () = { searchString = "" }

    let update (msg: Msg) (state: State) =
        match msg with
        | Search search -> { state with searchString = search }, Cmd.none

    let private iconBarView state dispatch gDispatch =
        StackPanel.create
            [ StackPanel.dock Dock.Top
              StackPanel.margin (Thickness.Parse("0, 10"))
              StackPanel.orientation Orientation.Horizontal
              StackPanel.children
                  [ Button.create
                      [ Button.classes [ "iconButton" ]
                        Button.content Icons.settings
                        Button.onClick (fun _ -> Global.OpenSettingsWindow false |> gDispatch) ] ] ]

    let private emptyGamesListView (state: State) (dispatch: Msg -> unit) =
        TextBlock.create
            [ Grid.row 1
              match state.searchString with
              | "" -> "You have no games installed."
              | _ -> "No installed game matches your search."
              |> TextBlock.text ]

    let private gameTemplateView startGame (game: InstalledGame) (dispatch: Msg -> unit) =
        Grid.create
            [ Grid.columnDefinitions "Auto, 1*"
              Grid.margin (Thickness.Parse("0, 8"))
              Grid.contextMenu
                  (ContextMenu.create
                      [ ContextMenu.viewItems
                          [ MenuItem.create
                              [ MenuItem.header "Start"
                                MenuItem.onClick (startGame game) ] ] ])
              Grid.children
                  [ StackPanel.create
                      [ StackPanel.children
                          [ TextBlock.create
                              [ TextBlock.textWrapping TextWrapping.Wrap
                                TextBlock.text game.name ]
                            TextBlock.create
                                [ TextBlock.isVisible (not game.updateable)
                                  TextBlock.textWrapping TextWrapping.Wrap
                                  TextBlock.text "(not updateable for now)"
                                  TextBlock.margin (Thickness.Parse("3,0,0,0")) ] ] ] ] ]

    let private filledGamesListView gameList startGame dispatch =
        ScrollViewer.create
            [ Grid.row 1
              ScrollViewer.horizontalScrollBarVisibility
                  Primitives.ScrollBarVisibility.Disabled
              ScrollViewer.content
                  (StackPanel.create
                      [ StackPanel.orientation Orientation.Vertical
                        StackPanel.margin (Thickness.Parse("10, 5"))
                        StackPanel.children
                            [ ItemsControl.create
                                [ ItemsControl.dataItems gameList
                                  ItemsControl.itemTemplate
                                      (DataTemplateView<InstalledGame>
                                          .create
                                              (fun game ->
                                                  gameTemplateView startGame game dispatch)) ] ] ]) ]

    let private gamesListView installedGames startGame state dispatch =
        let filteredGamesList =
            installedGames
            |> List.filter (fun game ->
                game.name.ToLower().Contains(state.searchString.ToLower()))

        match filteredGamesList.Length with
        | 0 -> emptyGamesListView state dispatch :> IView
        | _ -> filledGamesListView filteredGamesList startGame dispatch :> IView

    // TODO: icon
    let private menuItem currentMode gDispatch (text: string) badge mode =
        Button.create
            [ Button.classes [ if mode = currentMode then "active" else () ]
              Button.content
                (DockPanel.create
                    [ DockPanel.children
                        [ Border.create
                            [ Border.classes [ "badge" ]
                              Border.dock Dock.Right
                              Border.margin (5.0, 0.0, 0.0, 0.0)
                              Border.child (TextBlock.create [ TextBlock.text (badge |> string) ]) ]
                          TextBlock.create
                              [ TextBlock.text text
                                TextBlock.verticalAlignment VerticalAlignment.Center ] ] ])
              Button.onClick (fun _ -> Global.ChangeMode mode |> gDispatch) ]

    let private middleView state (gState: Global.State) _ gDispatch =
        let menuItem = menuItem gState.mode gDispatch

        ScrollViewer.create
            [ ScrollViewer.content
                (StackPanel.create
                    [ StackPanel.orientation Orientation.Vertical
                      StackPanel.children
                          [ menuItem "Owned Games" 7 Global.Empty
                            menuItem "Installed" gState.installedGames.Length Global.Installed ] ]) ]

    let private downloadTemplateView (downloadStatus: DownloadStatus) =
        Grid.create
            [ Grid.columnDefinitions "Auto"
              Grid.margin (0.0, 5.0)
              Grid.rowDefinitions "Auto, Auto, Auto"
              Grid.children
                  [ TextBlock.create
                      [ TextBlock.row 0
                        TextBlock.text downloadStatus.gameTitle ]
                    ProgressBar.create
                        [ Grid.row 1
                          ProgressBar.isVisible
                          <| not downloadStatus.installing
                          ProgressBar.maximum downloadStatus.fileSize
                          ProgressBar.value
                          <| double downloadStatus.downloaded ]
                    TextBlock.create
                        [ Grid.row 2
                          TextBlock.isVisible downloadStatus.installing
                          TextBlock.text "Installing..." ]
                    TextBlock.create
                        [ Grid.row 2
                          TextBlock.isVisible
                          <| not downloadStatus.installing
                          TextBlock.text
                          <| sprintf "%i MB / %i MB" downloadStatus.downloaded
                                 (int downloadStatus.fileSize) ] ] ]

    let private downloadsView (downloadList: DownloadStatus list) =
        StackPanel.create
            [ StackPanel.orientation Orientation.Vertical
              StackPanel.margin (Thickness.Parse "12, 12")
              StackPanel.children
                  [ ItemsControl.create
                      [ ItemsControl.dataItems downloadList
                        ItemsControl.itemTemplate
                            (DataTemplateView<DownloadStatus>.create downloadTemplateView) ]
                    TextBlock.create
                        [ TextBlock.isVisible (downloadList.Length = 0)
                          TextBlock.text "No downloads" ] ] ]

    let private bottomBarView (gState: Global.State) =
        StackPanel.create
            [ StackPanel.dock Dock.Bottom
              StackPanel.orientation Orientation.Vertical
              StackPanel.children
                  [ downloadsView gState.downloads
                    TextBlock.create
                        [ TextBlock.dock Dock.Bottom
                          TextBlock.fontSize 10.0
                          TextBlock.text Config.version ] ] ]

    let view state gState dispatch gDispatch =
        Border.create
            [ Border.classes [ "leftBar" ]
              Border.column 0
              Border.padding (5.0, 0.0)
              Border.child
                  (DockPanel.create
                      [ DockPanel.children
                          [ iconBarView state dispatch gDispatch
                            bottomBarView gState
                            middleView state gState dispatch gDispatch ] ]) ]
