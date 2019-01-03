module GogApi.DotNet.FSharp.Authentication

open HttpFs.Client

open GogApi.DotNet.FSharp.Base
open GogApi.DotNet.FSharp.Helpers

let redirectUri = "https://embed.gog.com/on_login_success?origin=client"

type TokenResponse = {
    expires_in: int;
    scope: string;
    token_type: string;
    access_token: string;
    user_id: string;
    refresh_token: string;
    session_id: string;
}

let createAuth refreshed response =
    match response with
    | Success response ->
        Auth { accessToken = response.access_token; refreshToken = response.refresh_token; refreshed = refreshed }
    | Failure _ ->
        NoAuth

let getBasicQueries () =
    [
        createQuery "client_id" "46899977096215655";
        createQuery "client_secret" "9d85c43b1482497dbbce61f6e4aa173a433796eeae2ca8c5f6129f2dc4de46d9";
    ]

let newToken (code :string) =
    getBasicQueries ()
    |> List.append [ createQuery "grant_type" "authorization_code" ]
    |> List.append [ createQuery "code" code ]
    |> List.append [ createQuery "redirect_uri" redirectUri ]
    |> makeBasicJsonRequest<TokenResponse> Get NoAuth <| "https://auth.gog.com/token"
    |> createAuth false

let refresh auth =
    getBasicQueries ()
    |> List.append [ createQuery "grant_type" "refresh_token" ]
    |> List.append [ createQuery "refresh_token" auth.refreshToken ]
    |> makeBasicJsonRequest<TokenResponse> Get NoAuth <| "https://auth.gog.com/token"
    |> createAuth true
