module Andromeda.AvaloniaApp.FSharp.Helpers.LinqHelper

open Microsoft.FSharp.Linq.RuntimeHelpers
open System
open System.Linq.Expressions

let toLinqHelper<'a, 'b, 'c when 'c :> Expression> fn =
    <@ Func<'a, 'b>fn @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<'c>

let toLinq<'a, 'b> fn =
    toLinqHelper<'a, 'b, Expression<Func<'a, 'b>>> fn
