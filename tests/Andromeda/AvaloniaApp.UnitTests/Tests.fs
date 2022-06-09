module Tests

open Expecto
open System

open GogApi.DomainTypes

open Andromeda.Core

let dummyAuthentication: Authentication =
    { accessExpires = DateTimeOffset.MinValue
      accessToken = ""
      refreshToken = "" }

let dummySettings: Settings =
    { cacheRemoval = NoRemoval
      gamePath = ""
      updateOnStartup = false }

let tests = testList "Components.Main.Update" []
