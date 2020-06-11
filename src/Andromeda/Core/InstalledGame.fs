namespace Andromeda.Core.FSharp

open Myriad.Plugins

[<Generator.Lenses("Lenses.Lens")>]
type InstalledGame =
    { id: GogApi.DotNet.FSharp.DomainTypes.ProductId
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
