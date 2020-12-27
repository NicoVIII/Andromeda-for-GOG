namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Avalonia
open Avalonia.Controls
open Avalonia.Diagnostics
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Elmish
open System.IO

open Andromeda.AvaloniaApp.AvaloniaHelper

module Settings =
    module Dialogs =
        let getFolderDialog currentPath =
            let dialog = OpenFolderDialog()

            dialog.Directory <- currentPath

            dialog.Title <- "Choose where to look for GOG games"
            dialog

    type IWindow =
        inherit IAndromedaWindow

        [<CLIEvent>]
        abstract OnSave: IEvent<IWindow * Settings>

        abstract Save: Settings -> unit

    type Msg =
        | OpenDialog
        | SetCacheRemoval of CacheRemovalPolicy
        | SetGamepath of string
        | SetUpdateOnStartup of bool
        | Save

    type State = Settings

    let init (settings: Settings) = settings, Cmd.none

    let isStateValid state = state.gamePath |> Directory.Exists

    let update (window: IWindow) (msg: Msg) (state: State) =
        match msg with
        | OpenDialog ->
            let dialog = Dialogs.getFolderDialog state.gamePath

            let showDialog window =
                async {
                    let! result = dialog.ShowAsync(window) |> Async.AwaitTask
                    // The dialog can return null, we throw an error to avoid updating the gamepath
                    if isNull result then failwith "Dialog was canceled"
                    return result
                }

            state, Cmd.OfAsync.perform showDialog (window :?> Window) SetGamepath
        | SetCacheRemoval policy -> { state with cacheRemoval = policy }, Cmd.none
        | SetGamepath gamePath -> { state with gamePath = gamePath }, Cmd.none
        | SetUpdateOnStartup update -> { state with updateOnStartup = update }, Cmd.none
        | Save ->
            // We only save, if state is valid
            if isStateValid state then window.Save state

            state, Cmd.none

    let view (state: State) (dispatch: Msg -> unit) =
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
                            ComboBox.dataItems [
                                NoRemoval
                                RemoveByAge 30u
                            ]
                            ComboBox.itemTemplate (
                                DataTemplateView<CacheRemovalPolicy>.create
                                <| (function
                                    | NoRemoval -> "No removal"
                                    | RemoveByAge age ->
                                        sprintf "Delete after %i days" age
                                    >> simpleTextBlock)
                            )
                            ComboBox.selectedItem state.cacheRemoval
                            ComboBox.onSelectedItemChanged
                                (fun x ->
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
                            ComboBox.onSelectedItemChanged
                                (fun x ->
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

    type SettingsWindow(settings: Settings) as this =
        inherit AndromedaWindow()

        let saveEvent = new Event<_>()

        do
            base.Title <- "Settings"
            base.WindowStartupLocation <- WindowStartupLocation.CenterOwner
            base.ShowInTaskbar <- false
            base.Width <- 600.0
            base.Height <- 260.0

#if DEBUG
            DevTools.Attach(this, Config.devToolGesture)
            |> ignore
#endif

            let updateWithServices = update this

            Program.mkProgram init updateWithServices view
            |> Program.withHost this
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWith settings

        interface IWindow with
            [<CLIEvent>]
            override __.OnSave = saveEvent.Publish

            member __.Save(settings: Settings) =
                saveEvent.Trigger(this :> IWindow, settings)
