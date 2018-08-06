# Andromeda-for-GOG
[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](http://www.repostatus.org/badges/latest/active.svg)](http://www.repostatus.org/#active)
[![GitHub Release](https://img.shields.io/github/release/NicoVIII/Andromeda-for-GOG.svg)](https://github.com/NicoVIII/Andromeda-for-GOG/releases/latest)
[![Github Pre-Release](https://img.shields.io/github/release/NicoVIII/Andromeda-for-GOG/all.svg?label=prerelease)](https://github.com/NicoVIII/Andromeda-for-GOG/releases)
[![GitHub License](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/NicoVIII/Andromeda-for-GOG/master/LICENSE.txt)

This project aims at providing a Galaxy-like client for GOG with the help of https://www.gog.com/forum/general/unofficial_gog_api_documentation/page1 also for Linux systems.

## Dependencies
TODO

## Development
### Building
(The build scripts are just for linux. You have to build this on other platforms manually)
To build this project, you can run build.sh. This will restore the whole solution (`dotnet restore`) and build the consoleapp using fake (`cd Andromeda.ConsoleApp && dotnet fake build`).
The script will automatically start the build, you can comment the line out to avoid this.

To start the program use dotnet and the build .dll: `dotnet ./bin/Debug/netcoreapp2.1/Andromeda.ConsoleApp.dll` or `dotnet ./Andromeda.ConsoleApp/bin/Debug/netcoreapp2.1/Andromeda.ConsoleApp.dll` depending on your location
