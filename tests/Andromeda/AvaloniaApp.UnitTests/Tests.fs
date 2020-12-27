module Tests

open Andromeda.Core.DomainTypes
open Expecto
open GogApi.DotNet.FSharp.DomainTypes
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

let tests =
    testList
        "Components.Main.Update"
        [ test "ChangeMode" {
            let state: Main.State =
                { authentication = dummyAuthentication
                  downloads = Map.empty
                  installedGames = Map.empty
                  mode = Empty
                  notifications = []
                  settings = dummySettings
                  terminalOutput = [] }

            let msg = Main.Msg.ChangeMode Installed
            let (actual, _, _) = Main.Update.update msg state
            let expected = { state with mode = Installed }
            Expect.equal actual expected "States should equal"
          } ]
