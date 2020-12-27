module Tests

open Expecto

open GogApi.DotNet.FSharp.DomainTypes
open Andromeda.Core

let tests =
    testList
        "Diverse"
        [ test "filterInstallersByOS" {
            let createDummyInfo (id, os) =
                { InstallerInfo.files = []
                  id = id
                  os = os
                  version = None }

            let infos =
                [ "lin1", "linux"
                  "win1", "windows"
                  "lin2", "linux"
                  "blaa", "blabla"
                  "mac1", "mac"
                  "mac2", "mac"
                  "win2", "windows" ]
                |> List.map createDummyInfo

            // Linux
            let infosExpected =
                [ "lin1", "linux"; "lin2", "linux" ]
                |> List.map createDummyInfo

            let filteredInfos =
                Diverse.filterInstallersByOS SystemInfo.OS.Linux infos

            Expect.equal filteredInfos infosExpected "The lists should equal"

            // Windows
            let infosExpected =
                [ "win1", "windows"; "win2", "windows" ]
                |> List.map createDummyInfo

            let filteredInfos =
                Diverse.filterInstallersByOS SystemInfo.OS.Windows infos

            Expect.equal filteredInfos infosExpected "The lists should equal"

            // MacOS
            let infosExpected =
                [ "mac1", "mac"; "mac2", "mac" ]
                |> List.map createDummyInfo

            let filteredInfos =
                Diverse.filterInstallersByOS SystemInfo.OS.MacOS infos

            Expect.equal filteredInfos infosExpected "The lists should equal"
          } ]
