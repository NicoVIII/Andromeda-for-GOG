# Andromeda-for-GOG

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](http://www.repostatus.org/badges/latest/active.svg)](http://www.repostatus.org/#active)
[![GitHub Release](https://img.shields.io/github/release/NicoVIII/Andromeda-for-GOG.svg)](https://github.com/NicoVIII/Andromeda-for-GOG/releases/latest)
[![Github Pre-Release](https://img.shields.io/github/release/NicoVIII/Andromeda-for-GOG/all.svg?label=prerelease)](https://github.com/NicoVIII/Andromeda-for-GOG/releases)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/075c69d86f154b40bef949483e04b98c)](https://www.codacy.com/manual/NicoVIII/Andromeda-for-GOG?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=NicoVIII/Andromeda-for-GOG&amp;utm_campaign=Badge_Grade)
[![GitHub License](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/NicoVIII/Andromeda-for-GOG/master/LICENSE)

This project aims at providing a Galaxy-like client for GOG with the help of <https://www.gog.com/forum/general/unofficial_gog_api_documentation/page1> also for Linux systems. It will focus the downloading, installing and updating of games at first.  
If you need something to play Multiplayer over Galaxy on linux, have a look at the comet project from the brilliant guy who started the unofficial documentation:
<https://gitlab.com/Yepoleb/comet>
There is an alternative for Linux systems written in Python called Minigalaxy:  
<https://github.com/sharkwouter/minigalaxy>

## Dependencies

Neither for AppImage nor for the single files executables are any dependencies required.

## Installation

Just download the archive for your os from the GitHub releases and unpack it anywhere you want.
On linux you could use the AppImage, if you want to.

## Usage

Simple start the Andromeda.FSharp(.exe) or the AppImage.

### Define settings

If you start Andromeda for the first time, you will get a settings window. Please configure your desired path for the games here. (For now it is just a plain text box, but it will check, if the directory exists)

### Install game

Press the workaround "Install game" button. Type the name of the game or a part of it. Try to type something, which matches only the one game you want to install.  
If it worked, you should see a status indicator of the download and installation.

### Upgrade games

To upgrade all of your installed games, press the "Upgrade games" button. If Andromeda finds updates, it will install them. There is for now no indication, if it does not find some.

### Start game

This does not work all the time, but you can try to right click the game in the left bar and click "Start" to start the game. The console output will be shown in the bottom right terminal.

## Development

[![Actions Status](https://github.com/NicoVIII/Andromeda-for-GOG/workflows/CI/badge.svg)](https://github.com/NicoVIII/Andromeda-for-GOG/actions)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/075c69d86f154b40bef949483e04b98c?branch=develop)](https://www.codacy.com/manual/NicoVIII/Andromeda-for-GOG?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=NicoVIII/Andromeda-for-GOG&amp;utm_campaign=Badge_Grade)

For for information about development have a look at [README-DEVELOPMENT](README-DEVELOPMENT.md).
