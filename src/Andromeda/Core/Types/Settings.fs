namespace Andromeda.Core.FSharp.DomainTypes

type CacheRemovalPolicy =
    | NoRemoval
    | RemoveWithAge of uint32

type SettingsV1 = { gamePath: string }

type Settings =
    { cacheRemoval: CacheRemovalPolicy
      gamePath: string }
