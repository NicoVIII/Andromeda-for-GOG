namespace Andromeda.Core

open System.Diagnostics

module System =
    /// Opens the given folder path in the explorer of the OS
    let openFolder folder =
        let startInfo = new ProcessStartInfo()
        startInfo.FileName <- folder
        startInfo.UseShellExecute <- true
        startInfo.Verb <- "open"
        Process.Start(startInfo) |> ignore
