namespace Andromeda.Core.FSharp.Helpers

[<RequireQualifiedAccess>]
module String =
    /// Helper function to use replace in a functional style
    let replace (oldValue: string) newValue (source: string) =
        source.Replace(oldValue, newValue)
