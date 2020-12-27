namespace Andromeda.Core.DomainTypes

open Myriad.Plugins

open Andromeda.Core.Lenses

type InstalledGame =
    { id: GogApi.DotNet.FSharp.DomainTypes.ProductId
      name: string
      path: string
      version: string
      updateable: bool
      image: string option }

module InstalledGame =
    let create id name path version =
        { InstalledGame.id = id
          name = name
          path = path
          version = version
          updateable = false
          image = None }

module InstalledGameLenses =
    let id =
        Lens
            ((fun (x: InstalledGame) -> x.id),
             (fun (x: InstalledGame) (value: GogApi.DotNet.FSharp.DomainTypes.ProductId) ->
                 { x with id = value }))

    let name =
        Lens
            ((fun (x: InstalledGame) -> x.name),
             (fun (x: InstalledGame) (value: string) -> { x with name = value }))

    let path =
        Lens
            ((fun (x: InstalledGame) -> x.path),
             (fun (x: InstalledGame) (value: string) -> { x with path = value }))

    let version =
        Lens
            ((fun (x: InstalledGame) -> x.version),
             (fun (x: InstalledGame) (value: string) -> { x with version = value }))

    let updateable =
        Lens
            ((fun (x: InstalledGame) -> x.updateable),
             (fun (x: InstalledGame) (value: bool) -> { x with updateable = value }))

    let image =
        Lens
            ((fun (x: InstalledGame) -> x.image),
             (fun (x: InstalledGame) (value: string option) -> { x with image = value }))
