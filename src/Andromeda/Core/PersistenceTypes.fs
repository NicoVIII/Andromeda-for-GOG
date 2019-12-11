namespace Andromeda.Core.FSharp

module PersistenceTypes =
    // TODO: Add Uniontypes for errors and return Result types
    type LoadAppData = unit -> AppData
    type SaveAppData = AppData -> bool

    type LoadSettings = unit -> Settings
    type SaveSettings = Settings -> unit
