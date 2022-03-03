namespace Andromeda.AvaloniaApp

open SimpleOptics

/// Optics to simplify usage of state
module MainStateOptic =
    let authentication =
        Lens((fun r -> r.authentication), (fun r v -> { r with authentication = v }))

    let downloads = Lens((fun r -> r.downloads), (fun r v -> { r with downloads = v }))

    let installedGames =
        Lens((fun r -> r.installedGames), (fun r v -> { r with installedGames = v }))

    let notifications =
        Lens((fun r -> r.notifications), (fun r v -> { r with notifications = v }))

    let settings = Lens((fun r -> r.settings), (fun r v -> { r with settings = v }))

    let terminalOutput =
        Lens((fun r -> r.terminalOutput), (fun r v -> { r with terminalOutput = v }))
