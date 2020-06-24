namespace Andromeda.Core.FSharp

open Andromeda.Core.FSharp.DomainTypes

open GogApi.DotNet.FSharp.DomainTypes

module PersistenceTypes =
    // TODO: Add Uniontypes for errors and return Result types
    type LoadAuthorizationData = unit -> Authentication

    type SaveAuthorizationData = Authentication -> bool

    type LoadSettings = unit -> Settings

    type SaveSettings = Settings -> unit
