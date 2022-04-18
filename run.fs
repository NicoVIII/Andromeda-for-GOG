open Fake.Core
open Fake.IO
open System.IO
open System.Net.Http

open RunHelpers
open RunHelpers.BasicShortcuts
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

    let framework = "net6.0"
    let appimagetoolVersion = "continuous"

let httpClient = new HttpClient()

module DotNet =
    let publishSelfContained outDir project version os =
        dotnet [
            "publish"
            "-r"
            DotNetOS.toString os
            "-v"
            "minimal"
            "-c"
            DotNetConfig.toString Release
            "-o"
            outDir
            "--self-contained"
            "/p:PublishSingleFile=true"
            "/p:PublishTrimmed=true"
            "/p:EnableCompressionInSingleFile=true"
            "/p:IncludeNativeLibrariesForSelfExtract=true"
            "/p:DebugType=None"
            $"/p:Version=%s{version}"
            project
        ]

module Task =
    let restore () =
        job {
            DotNet.toolRestore ()
            DotNet.restore Config.mainProject

            for proj in Config.testProjects do
                DotNet.restore proj
        }

    let buildApp () = DotNet.build Config.mainProject Debug

    let build () =
        job {
            buildApp ()

            for proj in Config.testProjects do
                DotNet.build proj Debug
        }

    let run () = DotNet.run Config.mainProject

    let runTest () =
        job {
            for proj in Config.testProjects do
                DotNet.run proj
        }

    let publishAsAppImage version =
        let result =
            job {
                dotnet [
                    "publish"
                    "-v"
                    "m"
                    "-c"
                    "Release"
                    "-f"
                    Config.framework
                    "-r"
                    "linux-x64"
                    "--self-contained"
                    "-p:PublishTrimmed=true"
                    "-p:DebugType=None"
                    $"/p:Version=%s{version}"
                    Config.mainProject
                ]

                Shell.mkdir "AppDir/usr"

                Shell.mv
                    $"src/Andromeda/AvaloniaApp/bin/Release/{Config.framework}/linux-x64/publish"
                    "AppDir/usr/bin"

                Internal.basicCommand
                    "cp"
                    [ "-a"
                      "assets/build/appimage/."
                      "AppDir" ]

                if not (File.exists "appimagetool-x86_64.AppImage") then
                    do
                        task {
                            let! response =
                                httpClient.GetAsync(
                                    $"https://github.com/AppImage/AppImageKit/releases/download/{Config.appimagetoolVersion}/appimagetool-x86_64.AppImage",
                                    HttpCompletionOption.ResponseHeadersRead
                                )

                            use file = File.OpenWrite "./appimagetool-x86_64.AppImage"
                            do! response.Content.CopyToAsync file
                        }
                        |> Async.AwaitTask
                        |> Async.RunSynchronously

                    Internal.basicCommand
                        "chmod"
                        [ "a+x"
                          "appimagetool-x86_64.AppImage" ]

                Internal.basicCommand
                    "./appimagetool-x86_64.AppImage"
                    [ "--appimage-extract-and-run"
                      "AppDir"
                      "-u"
                      "gh-releases-zsync|NicoVIII|Andromeda-for-GOG|latest|Andromeda-*.AppImage.zsync" ]

                Internal.basicCommand
                    "mv"
                    [ "Andromeda-x86_64.AppImage"
                      "./publish/Andromeda-x86_64.AppImage" ]

                Internal.basicCommand
                    "mv"
                    [ "Andromeda-x86_64.AppImage.zsync"
                      "./publish/Andromeda-x86_64.AppImage.zsync" ]

                printfn "Finished publishing as AppImage"

                // Clean up
                Internal.basicCommand "rm" [ "-rf"; "AppDir" ]
                |> ignore
            }

        result

    let publish version =
        let publish =
            DotNet.publishSelfContained Config.publishPath Config.mainProject version

        Shell.mkdir Config.publishPath
        Shell.cleanDir Config.publishPath

        job {
            publish LinuxX64

            Shell.mv
                $"%s{Config.publishPath}/%s{Config.projectName}"
                $"%s{Config.publishPath}/%s{Config.artifactName}-linux-x64"

            // Publish as AppImage
            publishAsAppImage version

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
        | [ "build-app" ] ->
            job {
                Task.restore ()
                Task.buildApp ()
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
        | [ "publish"; version ] ->
            job {
                Task.restore ()
                Task.publish version
            }
        // Errors for missing arguments
        | [ "publish" ] ->
            let msg = [ "Missing version argument!" ]
            Error(1, msg)
        | _ ->
            let msg =
                [ "Usage: dotnet run [<command>]"
                  "Look up available commands in run.fs" ]

            Error(1, msg)
    |> ProcessResult.wrapUp
