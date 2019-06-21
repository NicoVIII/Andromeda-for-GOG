namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.AvaloniaApp.FSharp.ViewModels
open Avalonia.Controls
open Avalonia.Controls.Templates
open System

type ViewLocator() =
    interface IDataTemplate with
        member this.SupportsRecycling = false
        member this.Match(data: obj) = data :? ViewModelBase

        member this.Build(data) =
            let name = data.GetType().FullName.Replace("ViewModel", "View");
            let ``type`` = Type.GetType(name);

            match ``type`` with
            | null ->
                let textBlock = TextBlock();
                textBlock.Text <- "Not Found: " + name
                textBlock :> IControl
            | x ->
                Activator.CreateInstance ``type`` :?> IControl
