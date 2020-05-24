namespace Andromeda.AvaloniaApp.FSharp

open Andromeda.Core.FSharp
open GogApi.DotNet.FSharp.DomainTypes

[<AutoOpen>]
module DomainTypes =
    type DownloadStatus =
        { gameId: ProductId
          gameTitle: string
          filePath: string
          fileSize: float
          installing: bool
          downloaded: int }
