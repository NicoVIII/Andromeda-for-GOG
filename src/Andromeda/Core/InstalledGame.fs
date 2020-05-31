namespace Andromeda.Core.FSharp

open FSharpPlus.Lens
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

    // Lenses
    let inline _icon f a =
        f a.icon
        <&> fun b -> { a with icon = b }
    let inline _updateable f a =
        f a.updateable
        <&> fun b -> { a with updateable = b }
