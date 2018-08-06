#!/bin/bash
dotnet restore
(
  cd Andromeda.ConsoleApp || exit
  dotnet fake build
)

# If you don't want to run the project on building, comment this line out
dotnet ./Andromeda.ConsoleApp/bin/Debug/netcoreapp2.1/Andromeda.ConsoleApp.dll
