#!/bin/bash
dotnet restore -s "https://www.myget.org/F/avalonia-ci/api/v2" -s "https://api.nuget.org/v3/index.json" Andromeda.AvaloniaApp.FSharp/ && dotnet build --no-restore Andromeda.AvaloniaApp.FSharp/
