# Development

![Visualization](images/diagram.svg)

## Setup (VSCode)

The only supported way to work on this is by using the provided devcontainer
(https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.vscode-remote-extensionpack).
Therefore you need VSCode and Docker installed on your system, the rest is happening inside the container.

If VS Code does not suggest to open the repository with the container, run the "Remote-Container:
Rebuild and Reopen in Container" Action. Everything you need is installed in there.

## Building

To build simply run `dotnet run build`.

## Publishing

Publishing is done automatically with every commit on develop and for every release tag.
You can build the executables by running `dotnet run publish`.
It is possible that you need additional dependencies to run this.
