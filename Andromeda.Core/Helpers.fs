module Andromeda.Core.FSharp.Helpers

open Couchbase.Lite

let exeFst fnc (a, b) = (fnc a, b)

let exeSnd fnc (a, b) = (a, fnc b)

let fluent fnc a =
    fnc a |> ignore
    a

let convertFromArrayObject fnc array  =
    let rec helper lst index fnc (array: ArrayObject)  =
        let out = (fnc index array)::lst
        if array.Count > index + 1 then
            helper out (index+1) fnc array
        else
            out
    helper [] 0 fnc array
