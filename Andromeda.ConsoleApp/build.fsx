#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target
//"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators //enables !! and globbing

// *** Define Targets ***
Target.create "Clean" (fun _ ->
    !! "bin/*"
    ++ "obj/*"
    -- "bin/CouchbaseLite/*" // Exclude savedata from cleaning for now
    |> File.deleteAll
)

Target.create "Build" (fun _ ->
    !! "*.*proj"
    |> Seq.iter (DotNet.build (fun parameters -> { parameters with Configuration = DotNet.Debug }))
)

Target.create "Publish" (fun _ ->
    !! "*.*proj"
    |> Seq.iter (DotNet.publish (fun parameters -> { parameters with Configuration = DotNet.Release }))
)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "Build"

"Clean"
  ==> "Publish"

// *** Start Build ***
Target.runOrDefault "Build"
