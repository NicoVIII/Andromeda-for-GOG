namespace Andromeda.Core.FSharp

open GogApi.DotNet.FSharp.DomainTypes

[<AutoOpen>]
module DomainTypes =
    // Provide an important type from GogApi over Andromeda.Core to avoid dependency of app to api
    type Authentication = GogApi.DotNet.FSharp.DomainTypes.Authentication

    type InstalledGame =
        { id: ProductId
          name: string
          path: string
          version: string
          updateable: bool
          icon: string option }

    type Settings =
        { gamePath: string }
