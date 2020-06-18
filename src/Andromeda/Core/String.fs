namespace Andromeda.Core.FSharp

[<RequireQualifiedAccess>]
module String =
    let replace (oldValue: string) newValue (source: string) =
        source.Replace(oldValue, newValue)
