namespace Andromeda.Core.FSharp

[<AutoOpen>]
module DomainTypes =
    // Provide an important type from GogApi over Andromeda.Core to avoid dependency of app to api
    type Authentication = GogApi.DotNet.FSharp.Types.Authentication

    type InstalledGame =
        { id: int
          name: string
          path: string
          version: string
          updateable: bool
          icon: string option }

    type Settings =
        { gamePath: string }
