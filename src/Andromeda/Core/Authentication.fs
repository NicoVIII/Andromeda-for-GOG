namespace Andromeda.Core.FSharp

open GogApi.DotNet.FSharp

module Authentication =
    let newToken = Authentication.newToken

    let refresh = Authentication.refreshAuthentication

    let withAutoRefresh = Authentication.withAutoRefresh
