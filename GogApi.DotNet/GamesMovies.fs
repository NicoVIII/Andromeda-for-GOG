module GogApi.DotNet.FSharp.GamesMovies

open HttpFs.Client

open GogApi.DotNet.FSharp.Request

type OwnedGamesResponse = {
    owned: int list;
}

let askForOwnedGameIds auth =
    makeRequest<OwnedGamesResponse> Get auth [] "https://embed.gog.com/user/data/games"

type GameDetailsResponse = {
    title: string;
    downloads: obj list list;
}

let askForGameInfo id auth =
    sprintf "https://embed.gog.com/account/gameDetails/%i.json" id
    |> makeRequest<GameDetailsResponse> Get auth [ ]
