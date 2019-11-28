module Andromeda.Core.FSharp.AppData

open GogApi.DotNet.FSharp.Authentication
open GogApi.DotNet.FSharp.Base

let createBasicAppData settings: AppData = { authentication = NoAuth; installedGames = []; settings = settings }
let withNewToken appData code = { appData with authentication = newToken(code) }
