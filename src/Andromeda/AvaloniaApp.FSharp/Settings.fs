namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.DomainTypes
open Avalonia
open Avalonia.Controls
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

    type State = { gamePath: string }

    type Msg =
        | SetGamepath of string
        | Save

    let stateToSettings state =
        match state.gamePath with
        | gamePath when gamePath |> Directory.Exists ->
            { Settings.gamePath = gamePath } |> Some
        | _ -> None

    let init (settings: Settings) =
        let state = { gamePath = settings.gamePath }

        state, Cmd.none

    let update (window: IWindow) (msg: Msg) (state: State) =
        match msg with
        | SetGamepath gamePath -> { state with gamePath = gamePath }, Cmd.none
        | Save ->
            match stateToSettings state with
            | Some settings -> window.Save settings
            | None -> ()
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
                    Button.create
                        [ Button.content "Save"
                          Button.isEnabled (stateToSettings state |> Option.isSome)
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
