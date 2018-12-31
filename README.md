# Andromeda-for-GOG
[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](http://www.repostatus.org/badges/latest/active.svg)](http://www.repostatus.org/#active)
[![GitHub Release](https://img.shields.io/github/release/NicoVIII/Andromeda-for-GOG.svg)](https://github.com/NicoVIII/Andromeda-for-GOG/releases/latest)
[![Github Pre-Release](https://img.shields.io/github/release/NicoVIII/Andromeda-for-GOG/all.svg?label=prerelease)](https://github.com/NicoVIII/Andromeda-for-GOG/releases)
[![CodeFactor](https://www.codefactor.io/repository/github/nicoviii/andromeda-for-gog/badge)](https://www.codefactor.io/repository/github/nicoviii/andromeda-for-gog)
[![GitHub License](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/NicoVIII/Andromeda-for-GOG/master/LICENSE)

This project aims at providing a Galaxy-like client for GOG with the help of https://www.gog.com/forum/general/unofficial_gog_api_documentation/page1 also for Linux systems. It will focus the downloading, installing and updating of games at first.  
If you need something to play Multiplayer over Galaxy on linux, have a look at the comet project from the brilliant guy who started the unofficial documentation:
https://gitlab.com/Yepoleb/comet

## Dependencies
You need .NET Core 2.2 to run Andromeda. You can download it here:
https://www.microsoft.com/net/download

## Installation
Just download the `.tar.xz`, `.tar.gz` or `.zip` file from the GitHub releases and unpack it anywhere you want.

## Usage
Execute `start.sh` or `start.cmd` (untested) to run the program.

For now I sadly did not find a way to make the browsery stuff work, so for now you have to live with some workarounds.

### Install game
Press the workaround "Install game" button. Type the name of the game or a part of it. Try to type something, which matches only the one game you want to install.  
If it worked, you should see a status indicator of the download and installation.

### Upgrade games
To upgrade all of your installed games, press the "Upgrade games" button. If Andromeda finds updates, it will install them. There is for now no indication, if it does not find some.

### Start game
This does not work all the time, but you can try to right click the game in the left bar and click "Start" to start the game.

## Development
[![pipeline status](https://gitlab.com/NicoVIII/Andromeda-for-GOG/badges/develop/pipeline.svg)](https://gitlab.com/NicoVIII/Andromeda-for-GOG/commits/develop)
[![CodeFactor](https://www.codefactor.io/repository/github/nicoviii/andromeda-for-gog/badge/develop)](https://www.codefactor.io/repository/github/nicoviii/andromeda-for-gog/overview/develop)

### Setup (VSCode)
TBD

### Building
To build the program use dotnet in the root folder: `dotnet build`
This will build the while solution.

To start the program use dotnet and the built `.dll`:  
`dotnet ./Andromeda.AvaloniaApp/bin/Debug/netcoreapp2.2/Andromeda.AvaloniaApp.dll`

### Versioning
I will try to stick to Semantic Versioning 2.0.0 (http://semver.org/spec/v2.0.0.html).

### Used Tools
I write the code in "Visual Studio Code" (https://code.visualstudio.com/).
