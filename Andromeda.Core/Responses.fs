module Andromeda.Core.FSharp.Responses

type TokenResponse = {
    expires_in: int;
    scope: string;
    token_type: string;
    access_token: string;
    user_id: string;
    refresh_token: string;
    session_id: string;
}

type OwnedGamesResponse = {
    owned: int list;
}

type UserDataResponse = {
    username: string;
    email: string;
}
