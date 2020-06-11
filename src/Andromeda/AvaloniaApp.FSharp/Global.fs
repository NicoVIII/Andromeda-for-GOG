namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp
open Andromeda.Core.FSharp.Lenses
open GogApi.DotNet.FSharp.DomainTypes

[<RequireQualifiedAccess>]
module Global =
    type Mode =
        | Empty
        | Installed

    /// <summary>
    /// Messages which are sendable from every component
    /// </summary>
    type Message =
        | ChangeMode of Mode
        | OpenSettingsWindow of initial: bool
        | StartGame of InstalledGame

    type State =
        { authentication: Authentication option
          downloads: DownloadStatus list
          installedGames: InstalledGame list
          mode: Mode
          settings: Settings option }

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
        { authentication = authentication
          downloads = []
          installedGames = []
          mode = Installed
          settings = settings }
