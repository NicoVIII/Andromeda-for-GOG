module Andromeda.Core.FSharp.Helpers

open System.Runtime.InteropServices

open Couchbase.Lite

let exeFst fnc (a, b) = (fnc a, b)

let exeSnd fnc (a, b) = (a, fnc b)

let fluent fnc a =
    fnc a |> ignore
    a

let convertFromArrayObject fnc array  =
    let rec helper lst index fnc (array: ArrayObject)  =
        if array.Count > index then
            let out = (fnc index array)::lst
            helper out (index+1) fnc array
        else
            lst
    helper [] 0 fnc array

type OS = Linux | MacOS | Windows | Unknown

let os =
    let isOS = RuntimeInformation.IsOSPlatform
    if isOS OSPlatform.Linux then Linux
    elif isOS OSPlatform.Windows then Windows
    elif isOS OSPlatform.OSX then MacOS
    else Unknown
