open Fake.IO

open RunHelpers
open RunHelpers.Templates

[<RequireQualifiedAccess>]
module Config =
    let projectName = "Andromeda.AvaloniaApp"

    let mainProject = "./src/Andromeda/AvaloniaApp/Andromeda.AvaloniaApp.fsproj"

    let testProjects =
        [ "./tests/Andromeda/AvaloniaApp.UnitTests/Andromeda.AvaloniaApp.UnitTests.fsproj"
          "./tests/Andromeda/Core.UnitTests/Andromeda.Core.UnitTests.fsproj" ]

    let artifactName = "Andromeda"

    let publishPath = "./publish"

module Task =
    let restore () =
        DotNet.restoreWithTools Config.mainProject

    let build () = DotNet.build Config.mainProject Debug
    let run () = DotNet.run Config.mainProject

    let runTest () =
        job {
            for proj in Config.testProjects do
                DotNet.run proj
        }

    let publish () =
        let publish = DotNet.publishSelfContained Config.publishPath Config.mainProject

        Shell.mkdir Config.publishPath
        Shell.cleanDir Config.publishPath

        job {
            publish LinuxX64

            Shell.mv
                $"%s{Config.publishPath}/%s{Config.projectName}"
                $"%s{Config.publishPath}/%s{Config.artifactName}-linux-x64"

            publish WindowsX64

            Shell.mv
                $"{Config.publishPath}/%s{Config.projectName}.exe"
                $"{Config.publishPath}/%s{Config.artifactName}-win-x64.exe"
        }

[<EntryPoint>]
let main args =
    args
    |> List.ofArray
    |> function
        | [ "restore" ] -> Task.restore ()
        | [ "build" ] ->
            job {
                Task.restore ()
                Task.build ()
            }
        | []
        | [ "run" ] ->
            job {
                Task.restore ()
                Task.run ()
            }
        | [ "test" ] ->
            job {
                Task.restore ()
                Task.runTest ()
            }
        | [ "publish" ] ->
            job {
                Task.restore ()
                Task.publish ()
            }
        | _ ->
            let msg =
                [ "Usage: dotnet run [<command>]"
                  "Look up available commands in run.fs" ]

            Error(1, msg)
    |> ProcessResult.wrapUp
