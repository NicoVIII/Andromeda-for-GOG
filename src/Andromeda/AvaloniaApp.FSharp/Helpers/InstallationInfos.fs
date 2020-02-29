namespace Andromeda.AvaloniaApp.FSharp.Helpers

open GogApi.DotNet.FSharp.GalaxyApi

type InstallationInfos (gameTitle, installerInfo) =
    member val InstallerInfo: InstallerInfo = installerInfo with get, set
    member val GameTitle: string = gameTitle with get, set
