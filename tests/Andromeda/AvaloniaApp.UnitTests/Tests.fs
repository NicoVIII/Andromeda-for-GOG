module Tests

open Andromeda.Core.DomainTypes
open Expecto
open GogApi.DomainTypes
open System

open Andromeda.AvaloniaApp
open Andromeda.AvaloniaApp.Components

let dummyAuthentication: Authentication =
    { accessExpires = DateTimeOffset.MinValue
      accessToken = ""
      refreshToken = "" }

let dummySettings: Settings =
    { cacheRemoval = NoRemoval
      gamePath = ""
      updateOnStartup = false }

let tests = testList "Components.Main.Update" []
