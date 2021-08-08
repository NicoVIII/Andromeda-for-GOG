# Andromeda-for-GOG

[![GitHub Release](https://img.shields.io/github/release/NicoVIII/Andromeda-for-GOG.svg)](https://github.com/NicoVIII/Andromeda-for-GOG/releases/latest)
[![GitHub Actions Build](https://img.shields.io/github/workflow/status/NicoVIII/Andromeda-for-GOG/Build)](https://github.com/NicoVIII/Andromeda-for-GOG/actions/workflows/build.yml)
![Last commit](https://img.shields.io/github/last-commit/NicoVIII/Andromeda-for-GOG)
[![Gitpod ready-to-code](https://img.shields.io/badge/Gitpod-ready--to--code-blue?logo=gitpod)](https://gitpod.io/#https://github.com/NicoVIII/Andromeda-for-GOG)

This project aims at providing a Galaxy-like client for GOG with the help of <https://www.gog.com/forum/general/unofficial_gog_api_documentation/page1> also for Linux systems. It will focus the downloading, installing and updating of games at first.

If you need something to play Multiplayer over Galaxy on linux, have a look at the comet project from the brilliant guy who started the unofficial documentation:
<https://gitlab.com/Yepoleb/comet>

There is an alternative for Linux systems written in Python called Minigalaxy:  
<https://github.com/sharkwouter/minigalaxy>

## Dependencies

Neither for AppImage nor for the single files executables are any dependencies required (as far as I know).

## Installation

Just download the executable for your os from the GitHub releases.
On linux you could use the AppImage, if you want to. It is updateable via AppImageUpdate.

## Usage

Simply start the downloaded executable.

### Define settings

If you start Andromeda for the first time, you should have a look at the settings of Andromeda.
Click on the cog and configure e.b. your desired path for the games here
(For now it is just a plain text box, but it will check, if the directory exists). You can also
alter the caching behaviour of Andromeda.

### Install game

Press the workaround "Install game" button. Type the name of the game or a part of it. You will get
a list of all games matching your search. Choose the one you want to install and click "Install".

If everything worked, you should see a status indicator of the download and installation.

### Upgrade games

To upgrade all of your installed games, press the "Upgrade games" button. If Andromeda finds updates,
it will install them. If there are no updates, you will see a notification at the top of the window.

### Start game

This does not work all the time, but you can try to right click the game and click "Start" to start
the game. The console output will be shown in the bottom right terminal.

## Development

For for information about development have a look at [README-DEVELOPMENT](README-DEVELOPMENT.md).
