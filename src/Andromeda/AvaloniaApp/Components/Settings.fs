namespace Andromeda.AvaloniaApp.Components

open Andromeda.Core
open Avalonia
open Avalonia.Controls
open Elmish
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout
open Avalonia.Platform.Storage
open System
open System.IO

open Andromeda.AvaloniaApp
open Andromeda.AvaloniaApp.AvaloniaHelper

module Settings =
    module Dialogs =
        let getFolderDialog (window: Window) currentPath =
            task {
                let! folder =
                    Uri currentPath |> window.StorageProvider.TryGetFolderFromPathAsync

                let options = FolderPickerOpenOptions()
                options.SuggestedStartLocation <- folder
                options.Title <- "Choose where to look for GOG games"
                return! window.StorageProvider.OpenFolderPickerAsync options
            }

    type State = Settings

    type Intent =
        | DoNothing
        | Save of State

    type Msg =
        | OpenDialog
        | SetCacheRemoval of CacheRemovalPolicy
        | SetGamepath of string
        | SetUpdateOnStartup of bool
        | Save

    let init (settings: Settings) = settings, Cmd.none

    let isStateValid (state: Settings) = state.gamePath |> Directory.Exists

    let update (window: Window) (msg: Msg) (state: State) =
        match msg with
        | OpenDialog ->
            let dialog = Dialogs.getFolderDialog window state.gamePath

            let showDialog window =
                async {
                    let! result = dialog |> Async.AwaitTask
                    // The dialog can return null, we throw an error to avoid updating the gamepath
                    if isNull result then
                        // TODO: Don't throw but simply don't update the gamepath
                        failwith "Dialog was canceled"

                    return result[0].Path.AbsolutePath
                }

            state, ElmishHelper.cmdOfAsync showDialog window SetGamepath, DoNothing
        | SetCacheRemoval policy ->
            { state with cacheRemoval = policy }, Cmd.none, DoNothing
        | SetGamepath gamePath -> { state with gamePath = gamePath }, Cmd.none, DoNothing
        | SetUpdateOnStartup update ->
            { state with updateOnStartup = update }, Cmd.none, DoNothing
        | Save ->
            let intent =
                // We only save, if state is valid
                if isStateValid state then Intent.Save state else DoNothing

            state, Cmd.none, intent

    module View =
        let render (state: State) (dispatch: Msg -> unit) : IView =
            StackPanel.create [
                StackPanel.margin 5.0
                StackPanel.orientation Orientation.Vertical
                StackPanel.spacing 5.0
                StackPanel.children [
                    simpleTextBlock "Game path"
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.spacing 5.0
                        StackPanel.children [
                            TextBox.create [
                                TextBox.text state.gamePath
                                TextBox.width 300.0
                                TextBox.onTextChanged (SetGamepath >> dispatch)
                            ]
                            Button.create [
                                Button.content ".."
                                Button.onClick (fun _ -> OpenDialog |> dispatch)
                            ]
                        ]
                    ]
                    simpleTextBlock "Cached installers removal"
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.spacing 5.0
                        StackPanel.children [
                            ComboBox.create [
                                ComboBox.width 300.0
                                ComboBox.dataItems [ NoRemoval; RemoveByAge 30u ]
                                ComboBox.itemTemplate (
                                    DataTemplateView<CacheRemovalPolicy>.create
                                    <| (function
                                        | NoRemoval -> "No removal"
                                        | RemoveByAge age ->
                                            sprintf "Delete after %i days" age
                                        >> simpleTextBlock)
                                )
                                ComboBox.selectedItem state.cacheRemoval
                                ComboBox.onSelectedItemChanged (fun x ->
                                    match box x with
                                    | :? CacheRemovalPolicy as policy ->
                                        policy |> SetCacheRemoval |> dispatch
                                    | _ -> failwith "Nope")
                            ]
                        ]
                    ]
                    simpleTextBlock "Update all games on startup"
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.spacing 5.0
                        StackPanel.children [
                            ComboBox.create [
                                ComboBox.width 300.0
                                ComboBox.dataItems [ true; false ]
                                ComboBox.itemTemplate (
                                    DataTemplateView<bool>.create
                                    <| (function
                                        | true -> "Yes"
                                        | false -> "No"
                                        >> simpleTextBlock)
                                )
                                ComboBox.selectedItem state.updateOnStartup
                                ComboBox.onSelectedItemChanged (fun x ->
                                    match box x with
                                    | :? bool as update ->
                                        update |> SetUpdateOnStartup |> dispatch
                                    | _ -> failwith "Nope")
                            ]
                        ]
                    ]
                    Button.create [
                        Button.content "Save"
                        Button.isEnabled (isStateValid state)
                        Button.onClick (fun _ -> Save |> dispatch)
                    ]
                ]
            ]
            :> IView
