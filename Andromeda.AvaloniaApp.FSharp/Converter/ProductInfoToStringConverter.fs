namespace Andromeda.AvaloniaApp.FSharp.Converter

open Avalonia.Data.Converters
open GogApi.DotNet.FSharp.Listing
open Microsoft.FSharp.Core

type ProductInfoToStringConverter () =
    interface IValueConverter with
        member __.Convert (value, _, _, _) =
            match value with
            | null -> null
            | :? ProductInfo as value -> value.title :> obj
            | _ -> invalidArg "value" ""

        member __.ConvertBack (_, _, _, _) =
            invalidOp ""
