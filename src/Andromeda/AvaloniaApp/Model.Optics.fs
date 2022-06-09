namespace Andromeda.AvaloniaApp

open SimpleOptics
open SimpleOptics.Presets

open Andromeda.Core

/// Optics to simplify usage of state
module MainStateOptic =
    // Base lenses
    let authentication =
        Lens((fun r -> r.authentication), (fun r v -> { r with authentication = v }))

    let games = Lens((fun r -> r.games), (fun r v -> { r with games = v }))

    let notifications =
        Lens((fun r -> r.notifications), (fun r v -> { r with notifications = v }))

    let settings = Lens((fun r -> r.settings), (fun r v -> { r with settings = v }))

    let terminalOutput =
        Lens((fun r -> r.terminalOutput), (fun r v -> { r with terminalOutput = v }))

    // Composed lenses
    let game productId =
        Optic.compose games (MapOptic.find productId)

    let gameStatus productId =
        Optic.compose (game productId) (GameOptic.status)

    let gameOutput productId =
        Optic.compose terminalOutput (MapOptic.find productId)
