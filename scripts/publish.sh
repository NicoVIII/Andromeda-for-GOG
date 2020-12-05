#!/bin/bash
echo "Clear deploy folder."
rm -fr "deploy"
mkdir -p "deploy"

DEPLOYNAME="Andromeda-v$1"
FRAMEWORK="netcoreapp3.1"

echo "Start publishing as single file executables."
(
  cd "src/Andromeda/AvaloniaApp.FSharp" || exit

  # Restore
  dotnet tool restore
  dotnet paket restore

  echo "Build for Linux."
  dotnet publish -v quiet -c Release -r linux-x64 -o "../../../deploy" -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None
  mv "../../../deploy/Andromeda.AvaloniaApp.FSharp" "../../../deploy/$DEPLOYNAME-linux-x64"

  echo "Build for Windows."
  dotnet publish -v quiet -c Release -r win-x64 -o "../../../deploy" -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None
  mv "../../../deploy/Andromeda.AvaloniaApp.FSharp.exe" "../../../deploy/$DEPLOYNAME-win-x64.exe"

  echo "Build for macOS."
  dotnet publish -v quiet -c Release -r osx-x64 -o "../../../deploy" -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=None
  mv "../../../deploy/Andromeda.AvaloniaApp.FSharp" "../../../deploy/$DEPLOYNAME-osx-x64"

  echo "Finished publishing as single file executables."
)

echo "Start publishing as AppImage."
(
  cd "src/Andromeda/AvaloniaApp.FSharp" || exit
  dotnet publish -v quiet -c Release -f "$FRAMEWORK" -r ubuntu.16.04-x64 -p:PublishTrimmed=true -p:DebugType=None
  mkdir -p "AppDir/usr"
  mv -T "bin/Release/$FRAMEWORK/ubuntu.16.04-x64/publish" "AppDir/usr/bin"

  cp -a "../../../assets/build/appimage/." "AppDir"
)
if ! [ -f "./appimagetool-x86_64.AppImage" ]
then
  wget "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
  chmod a+x appimagetool-x86_64.AppImage
fi
./appimagetool-x86_64.AppImage --appimage-extract
(
  cd "src/Andromeda/AvaloniaApp.FSharp" || exit
  ./../../../squashfs-root/AppRun --no-appstream ./AppDir -u "gh-releases-zsync|NicoVIII|Andromeda-for-GOG|latest|Andromeda-*.AppImage.zsync"
  mv "Andromeda-x86_64.AppImage" "../../../deploy/Andromeda-x86_64.AppImage"
  mv "Andromeda-x86_64.AppImage.zsync" "../../../deploy/Andromeda-x86_64.AppImage.zsync"
  rm -r "AppDir"

  echo "Finished publishing as AppImage."
)
rm -r squashfs-root
