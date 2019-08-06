module Andromeda.Core.FSharp.InstalledGame

let create id name path version =
    { InstalledGame.id = id; name = name; path = path; version = version; updateable = false; icon = None }

let setUpdateable value game =
    { game with updateable = value}

let setIcon iconpath game =
    { game with icon = iconpath }
