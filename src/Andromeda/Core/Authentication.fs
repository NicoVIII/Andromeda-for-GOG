namespace Andromeda.Core.FSharp

open GogApi.DotNet.FSharp

module Authentication =
    let getNewToken = Authentication.getNewToken

    let getRefreshToken = Authentication.getRefreshToken

    let withAutoRefresh = Helpers.withAutoRefresh
