module GogApi.DotNet.FSharp.Helpers

type Result<'TSuccess,'TFailure> =
    | Success of 'TSuccess
    | Failure of 'TFailure
