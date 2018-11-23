module Andromeda.Core.FSharp.Helpers

open System.Runtime.InteropServices

open Couchbase.Lite

let exeFst fnc (a, b) = (fnc a, b)

let exeSnd fnc (a, b) = (a, fnc b)

/// <summary>Execute function and return input instead of output.</summary>
/// <param name="fnc">Function to execute</param>
/// <param name="input">Input to function and output of helper</param>
let fluent fnc input =
    fnc input |> ignore
    input

let convertFromArrayObject fnc array  =
    let rec helper lst index fnc (array: ArrayObject)  =
        if array.Count > index then
            let out = (fnc index array)::lst
            helper out (index+1) fnc array
        else
            lst
    helper [] 0 fnc array

type OS = Linux | MacOS | Windows

let os =
    let isOS = RuntimeInformation.IsOSPlatform
    if isOS OSPlatform.Linux then Linux
    elif isOS OSPlatform.Windows then Windows
    elif isOS OSPlatform.OSX then MacOS
    else failwith "I couldn't determine your OS? :O"
