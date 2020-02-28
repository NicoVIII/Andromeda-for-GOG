namespace Andromeda.AvaloniaApp.FSharp

[<AutoOpen>]
module DomainTypes =
    type DownloadStatus =
        { gameId: int
          gameTitle: string
          filePath: string
          fileSize: float
          installing: bool
          downloaded: int }
