namespace Andromeda.AvaloniaApp.FSharp.Helpers

open Andromeda.Core.FSharp.AppData

(**
This is a wrapper class for appdata to ensure,
that there exists only one reference, which can be changed.
*)
type AppDataWrapper(appData) =
    member val AppData: AppData = appData with get, set
