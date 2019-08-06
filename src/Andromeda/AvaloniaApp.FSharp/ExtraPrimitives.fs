namespace Andromeda.AvaloniaApp.FSharp

// Taken from www.fssnip.net/ne/title/Safely-unbox-to-an-option
[<AutoOpen>]
module ExtraPrimitives =
    let inline tryUnbox<'a> (x:obj) =
        match x with
        | :? 'a as result -> Some (result)
        | _ -> None
