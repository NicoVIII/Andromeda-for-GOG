namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Elmish
open System.IO

module Settings =
    type IWindow =
        inherit IAndromedaWindow

        [<CLIEvent>]
        abstract OnSave: IEvent<IWindow * Settings>

        abstract Save: Settings -> unit

    type Msg =
        | SetCacheRemoval of CacheRemovalPolicy
        | SetGamepath of string
        | Save

    type State = Settings

    let init (settings: Settings) =
        settings, Cmd.none

    let update (window: IWindow) (msg: Msg) (state: State) =
        match msg with
        | SetCacheRemoval policy -> { state with cacheRemoval = policy }, Cmd.none
        | SetGamepath gamePath -> { state with gamePath = gamePath }, Cmd.none
        | Save ->
            window.Save state
            state, Cmd.none

    let view (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.margin 5.0
              StackPanel.orientation Orientation.Vertical
              StackPanel.spacing 5.0
              StackPanel.children
                  [ TextBlock.create [ TextBlock.text "Game path" ]
                    StackPanel.create
                        [ StackPanel.orientation Orientation.Horizontal
                          StackPanel.spacing 5.0
                          StackPanel.children
                              [ TextBox.create
                                  [ TextBox.text state.gamePath
                                    TextBox.width 300.0
                                    TextBox.onTextChanged (SetGamepath >> dispatch) ] ] ]
                    TextBlock.create [ TextBlock.text "Cached installers removal" ]
                    StackPanel.create
                        [ StackPanel.orientation Orientation.Horizontal
                          StackPanel.spacing 5.0
                          StackPanel.children
                              [ ComboBox.create
                                  [ ComboBox.width 300.0
                                    ComboBox.dataItems [NoRemoval; RemoveByAge 30u]
                                    ComboBox.itemTemplate (DataTemplateView<CacheRemovalPolicy>.create
                                      <| fun policy ->
                                          let text =
                                            match policy with
                                            | NoRemoval -> "No removal"
                                            | RemoveByAge age -> sprintf "Delete after %i days" age
                                          TextBlock.create [ TextBlock.text text ])
                                    ComboBox.selectedItem state.cacheRemoval
                                    ComboBox.onSelectedItemChanged (fun x ->
                                        match box x with
                                        | :? CacheRemovalPolicy as policy ->
                                            policy |> SetCacheRemoval |> dispatch
                                        | _ -> failwith "Nope") ] ] ]
                    Button.create
                        [ Button.content "Save"
                          Button.onClick (fun _ -> Save |> dispatch) ] ] ]

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
            this.AttachDevTools(KeyGesture(Key.F12))
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
