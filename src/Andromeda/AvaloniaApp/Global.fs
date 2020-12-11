namespace Andromeda.AvaloniaApp

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Andromeda.Core.Lenses
open GogApi.DotNet.FSharp.DomainTypes

[<RequireQualifiedAccess>]
module Global =
    type Mode =
        | Empty
        | Installed

    type State =
        { authentication: Authentication option
          downloads: DownloadStatus list
          installedGames: InstalledGame list
          mode: Mode
          settings: Settings }

    /// <summary>
    /// Messages which are sendable from every component
    /// </summary>
    type Msg<'T> =
        | UseLens of Lens<State, 'T> * 'T
        | Authenticate of Authentication
        | ChangeMode of Mode
        | OpenSettingsWindow of Authentication
        | StartGame of InstalledGame

    module StateLenses =
        // Lenses
        let authentication =
            Lens((fun r -> r.authentication), (fun r v -> { r with authentication = v }))

        let downloads =
            Lens((fun r -> r.downloads), (fun r v -> { r with downloads = v }))

        let installedGames =
            Lens((fun r -> r.installedGames), (fun r v -> { r with installedGames = v }))

        let mode =
            Lens((fun r -> r.mode), (fun r v -> { r with mode = v }))

        let settings =
            Lens((fun r -> r.settings), (fun r v -> { r with settings = v }))

    let init authentication settings =
        let settings =
            match settings with
            | Some settings -> settings
            | None -> SystemInfo.defaultSettings ()

        let installedGames =
            match authentication with
            | Some authentication -> Installed.searchInstalled settings authentication
            | None -> []

        // After determining our settings, we perform a cache check
        Cache.check settings

        { authentication = authentication
          downloads = []
          installedGames = installedGames
          mode = Installed
          settings = settings }
