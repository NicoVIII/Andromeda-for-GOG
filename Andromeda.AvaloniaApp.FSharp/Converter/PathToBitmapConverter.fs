namespace Andromeda.AvaloniaApp.FSharp.Converter

open Avalonia.Data.Converters
open Avalonia.Media.Imaging
open Microsoft.FSharp.Core

type PathToBitmapConverter () =
    interface IValueConverter with
        member __.Convert(value, _, _, _) =
            match value with
            | null -> null
            | value ->
                // Unwrap optiontype if necessary
                let value =
                    match value with
                    | :? Option<string> as value ->
                        value.Value
                    | :? string as value->
                        value
                    | _ -> invalidArg "value" "no float or float option"
                new Bitmap(value) :> obj

        member __.ConvertBack(value, _, _, _) =
            (value :?> Bitmap).ToString() :> obj
