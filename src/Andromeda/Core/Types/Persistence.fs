namespace Andromeda.Core

open GogApi.DotNet.FSharp.DomainTypes

open Andromeda.Core.DomainTypes

module PersistenceTypes =
    // TODO: Add Uniontypes for errors and return Result types
    type LoadAuthorizationData = unit -> Authentication

    type SaveAuthorizationData = Authentication -> bool

    type LoadSettings = unit -> Settings

    type SaveSettings = Settings -> unit
