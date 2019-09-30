module Andromeda.Core.FSharp.AppData

open GogApi.DotNet.FSharp.Authentication
open GogApi.DotNet.FSharp.Base

let createBasicAppData (): AppData = { authentication = NoAuth; installedGames = []; settings = Settings.tmpDefault }
let withNewToken appData code = { appData with authentication = newToken(code) }
