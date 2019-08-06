#!/bin/bash
PROJECT_PATH="src/Andromeda/AvaloniaApp"
dotnet restore -s "https://www.myget.org/F/avalonia-ci/api/v2" -s "https://api.nuget.org/v3/index.json" "$PROJECT_PATH" && dotnet build --no-restore "$PROJECT_PATH"
