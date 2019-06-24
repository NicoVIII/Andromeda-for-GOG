namespace Andromeda.AvaloniaApp.FSharp.Converter

open Avalonia.Data.Converters
open Microsoft.FSharp.Core
open System

type FloatToStringConverter () =
    interface IValueConverter with
        member __.Convert(value, _, _, _) =
            match value with
            | null -> null
            | value ->
                // Unwrap optiontype if necessary
                let value =
                    match value with
                    | :? Option<float> as value ->
                        value.Value
                    | :? float as value->
                        value
                    | _ -> invalidArg "value" "no float or float option"
                String.Format("{0:0.#}", value) :> obj

        member __.ConvertBack(_, _, _, _) =
            invalidOp ""
