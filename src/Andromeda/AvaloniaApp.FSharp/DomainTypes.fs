namespace Andromeda.AvaloniaApp.FSharp

open GogApi.DotNet.FSharp.Types

[<AutoOpen>]
module DomainTypes =
    type DownloadStatus =
        { gameId: ProductId
          gameTitle: string
          filePath: string
          fileSize: float
          installing: bool
          downloaded: int }
