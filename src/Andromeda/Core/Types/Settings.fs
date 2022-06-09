namespace Andromeda.Core

type CacheRemovalPolicy =
    | NoRemoval
    | RemoveByAge of maxage: uint32

type SettingsV1 = { gamePath: string }

type SettingsV2 =
    { cacheRemoval: CacheRemovalPolicy
      gamePath: string }

type Settings =
    { cacheRemoval: CacheRemovalPolicy
      gamePath: string
      updateOnStartup: bool }
