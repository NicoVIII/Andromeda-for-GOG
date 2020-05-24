namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp
open FSharpPlus.Lens

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
          mode: Mode }

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

    let init authentication =
        { authentication = authentication
          downloads = []
          installedGames = []
          mode = Empty }
