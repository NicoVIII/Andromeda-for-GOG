cd Andromeda.ConsoleApp
dotnet restore
dotnet fake build

# If you don't want to run the project on building, comment this line out
dotnet ./bin/Debug/netcoreapp2.1/Andromeda.ConsoleApp.dll
