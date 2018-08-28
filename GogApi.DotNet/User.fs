module GogApi.DotNet.FSharp.User

open HttpFs.Client

open GogApi.DotNet.FSharp.Request

type UserDataResponse = {
    username: string;
    email: string;
}

let askForUserData auth =
    makeRequest<UserDataResponse> Get auth [] "https://embed.gog.com/userData.json"
