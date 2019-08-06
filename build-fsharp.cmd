SET project_path="src/Andromeda/AvaloniaApp.FSharp/"
dotnet restore -s "https://www.myget.org/F/avalonia-ci/api/v2" -s "https://api.nuget.org/v3/index.json" %project_path% && dotnet build --no-restore %project_path%
