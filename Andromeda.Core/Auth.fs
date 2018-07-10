module Andromeda.Core.FSharp.Auth

open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Responses
open HttpFs.Client

let redirectUri = "https://embed.gog.com/on_login_success?origin=client"

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
    |> makeRequest<TokenResponse> Get Empty <| "https://auth.gog.com/token"
