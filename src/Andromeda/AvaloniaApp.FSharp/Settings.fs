namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp
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
    type ISettingsWindow =
        abstract Close: unit -> unit

        [<CLIEvent>]
        abstract OnSave: IEvent<ISettingsWindow * Settings>
        abstract Save: Settings -> unit

    type State =
        { gamePath: string
          window: ISettingsWindow }

    type Msg =
        | SetGamepath of string
        | Save

    let stateToSettings state =
        match state.gamePath with
        | gamePath when gamePath |> Directory.Exists ->
            { gamePath = gamePath } |> Some
        | _ ->
            None

    let init (settings: Settings option, window: ISettingsWindow) =
        let state =
            match settings with
            | Some settings ->
                { gamePath = settings.gamePath
                  window = window }
            | None ->
                { gamePath = ""
                  window = window }
        state, Cmd.none

    let update (msg: Msg) (state: State) =
        match msg with
        | SetGamepath gamePath -> { state with gamePath = gamePath }, Cmd.none
        | Save ->
            match stateToSettings state with
            | Some settings ->
                state.window.Save settings
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
                              [ TextBox.create [
                                    TextBox.text state.gamePath
                                    TextBox.width 300.0
                                    TextBox.onTextChanged (SetGamepath >> dispatch) ] ] ]
                    Button.create
                        [ Button.content "Save"
                          Button.isEnabled (stateToSettings state |> Option.isSome)
                          Button.onClick (fun _ -> Save |> dispatch) ] ] ]

    type SettingsWindow(settings: Settings option) as this =
        inherit HostWindow()

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

            Program.mkProgram init update view
            |> Program.withHost this
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWith (settings, this)

        interface ISettingsWindow with
            member __.Close () = this.Close()

            [<CLIEvent>]
            member __.OnSave = saveEvent.Publish
            member __.Save(settings: Settings) = saveEvent.Trigger(this :> ISettingsWindow, settings)
