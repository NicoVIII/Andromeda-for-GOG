namespace Andromeda.Core

open System.Diagnostics

module System =
    let openFolder folder =
        let startInfo = new ProcessStartInfo()
        startInfo.FileName <- folder
        startInfo.UseShellExecute <- true
        startInfo.Verb <- "open"
        Process.Start(startInfo) |> ignore
