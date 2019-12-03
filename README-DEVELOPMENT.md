# Development

## Setup (VSCode)

Run "restore" task, after that you can use the debugger and run the build task.

For some reason the used build of Avalonia sometimes has problems with Omnisharp.
To make code completion work again, you have to comment out the first "UsingTask"-Element in `AvaloniaBuildTasks.targets` in NuGet-Download.  
For me this is located at `~/.nuget/packages/avalonia/[version]/build/AvaloniaBuildTasks.targets`. 

## Building

You need .NET Core 3.0 to build Andromeda. You can use the build task inside of VS Code to build the project.

## Publishing

Publishing only works on a linux system. I personally use Manjaro Linux or Ubuntu 19.10 (depending on the machine I use).  
You need additionally to building dependencies, zsyncmake (in the zsync package I guess) and appstream.

There is a bash script which bundles Andromeda in all the designated forms. Run `publish.sh` for that.

## Versioning

I will try to stick to Semantic Versioning 2.0.0 (<http://semver.org/spec/v2.0.0.html>).

## Used Tools

I write the code in "Visual Studio Code" (<https://code.visualstudio.com/>).
