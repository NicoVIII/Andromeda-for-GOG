namespace Andromeda.AvaloniaApp.FSharp.Helpers

open ReactiveUI

type DownloadStatus(gameTitle, path, fileSize) =
    inherit ReactiveObject()

    let mutable downloaded = 0
    let mutable installing = false

    member val GameTitle: string = gameTitle
    member val FilePath: string = path with get,set
    member val FileSize: float = fileSize
    member this.Downloaded
        with get () = downloaded
        and private set value = this.RaiseAndSetIfChanged(&downloaded, value) |> ignore
    member this.Installing
        with get () = installing
        and private set value = this.RaiseAndSetIfChanged(&installing, value) |> ignore

    member this.UpdateDownloaded downloaded = this.Downloaded <- downloaded
    member this.IndicateInstalling () = this.Installing <- true
