module Andromeda.AvaloniaApp.FSharp.Helpers.LinqHelper

open Microsoft.FSharp.Linq.RuntimeHelpers
open System
open System.Linq.Expressions

let toLinq fn =
    <@ Func<'a, 'b>fn @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<'a, 'b>>>
