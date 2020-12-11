namespace Andromeda.Core.DomainTypes

type CacheRemovalPolicy =
    | NoRemoval
    | RemoveByAge of maxage: uint32

type SettingsV1 = { gamePath: string }

type Settings =
    { cacheRemoval: CacheRemovalPolicy
      gamePath: string }
