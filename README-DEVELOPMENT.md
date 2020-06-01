# Development

## Setup (VSCode)

Run "restore" task, after that you can use the debugger and run the build task.

## Building

You need .NET Core 3.1 to build Andromeda. You can use the build task inside of VS Code to build the project.

## Publishing

Publishing only works on a linux system. It is tested on Manjaro Linux.  
You need additionally to building dependencies zsyncmake (in the zsync package I guess) and appstream.

There is a bash script which bundles Andromeda in all the designated forms. Run `./scripts/publish.sh` for that.

## Versioning

This project uses Semantic Versioning 2.0.0 (<http://semver.org/spec/v2.0.0.html>).

## Used Tools

The code is written in "Visual Studio Code" (<https://code.visualstudio.com/>). There is a .devcontainer defined, which you can use from within VScode or from a service like GitHub Codespaces.
