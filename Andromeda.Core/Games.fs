module Andromeda.Core.FSharp.Games

open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Responses
open HttpFs.Client

type GameId = GameId of int

let getOwnedGameIds auth =
    makeRequest<OwnedGamesResponse> Get auth [] "https://embed.gog.com/user/data/games"
    |> function
        | Some { owned = owned } -> owned
        | None -> []
    |> List.map GameId
