namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp
open FSharpPlus.Lens
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

    // Lenses
    let inline _authentication f s =
        f s.authentication
        <&> fun a -> { s with authentication = a }

    let inline _downloads f s =
        f s.downloads
        <&> fun d -> { s with downloads = d }

    let inline _installedGames f s =
        f s.installedGames
        <&> fun d -> { s with installedGames = d }

    let inline _mode f s =
        f s.mode <&> fun m -> { s with mode = m }

    let inline _settings f s =
        f s.settings
        <&> fun a -> { s with settings = a }

    let init authentication settings =
        { authentication = authentication
          downloads = []
          installedGames = []
          mode = Installed
          settings = settings }
