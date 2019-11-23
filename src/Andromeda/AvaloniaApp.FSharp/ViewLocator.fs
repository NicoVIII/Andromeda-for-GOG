namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.AvaloniaApp.FSharp.ViewModels
open Avalonia.Controls
open Avalonia.Controls.Templates
open System

type ViewLocator() =
    interface IDataTemplate with
        member __.SupportsRecycling = false
        member __.Match(data: obj) = data :? ViewModelBase

        member __.Build(data) =
            let name = data.GetType().FullName.Replace("ViewModel", "View");
            let ``type`` = Type.GetType(name);

            match ``type`` with
            | null ->
                let textBlock = TextBlock();
                textBlock.Text <- "Not Found: " + name
                textBlock :> IControl
            | _ ->
                Activator.CreateInstance ``type`` :?> IControl
