namespace Andromeda.AvaloniaApp.FSharp.Converter

open Avalonia.Data.Converters
open Microsoft.FSharp.Core

type OptionToBoolConverter () =
    interface IValueConverter with
        member __.Convert(value, _, _, _) =
            let inline optionToBool option =
                match option with
                | Some _ -> true
                | None -> false

            match tryUnbox<string option> value with
            | Some value -> optionToBool value |> box
            | None ->
                match tryUnbox<obj option> value with
                | Some value -> optionToBool value |> box
                | None -> invalidOp ""

        member __.ConvertBack(_, _, _, _) =
            invalidOp ""
