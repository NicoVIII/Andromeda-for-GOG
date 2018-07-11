module Andromeda.Core.FSharp.Helpers

let exeFst fnc (a, b) = (fnc a, b)

let exeSnd fnc (a, b) = (a, fnc b)
