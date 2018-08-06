#!/bin/bash
dotnet restore
cd Andromeda.ConsoleApp
  dotnet fake build
  cd ..

# If you don't want to run the project on building, comment this line out
dotnet ./Andromeda.ConsoleApp/bin/Debug/netcoreapp2.1/Andromeda.ConsoleApp.dll
