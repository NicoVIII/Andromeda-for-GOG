namespace Andromeda.Core.FSharp.Helpers

open System
open System.IO

module ResultHelper =
    type ResultBuilder() =
        member __.Return x = Ok x
        member __.Zero() = Ok()
        member __.Bind(xResult, f) = Result.bind f xResult

    let result = ResultBuilder()
