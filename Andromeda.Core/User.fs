module Andromeda.Core.FSharp.User

open Andromeda.Core.FSharp.Basics
open Andromeda.Core.FSharp.Responses
open HttpFs.Client

let getUserData auth =
    makeRequest<UserDataResponse> Get auth [] "https://embed.gog.com/userData.json"
