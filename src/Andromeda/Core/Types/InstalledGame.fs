namespace Andromeda.Core.FSharp.DomainTypes

open Andromeda.Core.FSharp.Lenses

open GogApi.DotNet.FSharp.DomainTypes

type InstalledGame =
    { id: ProductId
      name: string
      path: string
      version: string
      updateable: bool
      icon: string option }

module InstalledGame =
    let create id name path version =
        { InstalledGame.id = id
          name = name
          path = path
          version = version
          updateable = false
          icon = None }

module InstalledGameLenses =
    // Lenses
    let icon =
        Lens((fun r -> r.icon), (fun r v -> { r with icon = v }))

    let updateable =
        Lens((fun r -> r.updateable), (fun r v -> { r with updateable = v }))
