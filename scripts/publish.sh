#!/bin/bash
echo "Clear deploy folder."
rm -fr "deploy"
mkdir -p "deploy"

if [ $1 = 'dev' ]
then
  DEPLOYNAME="Andromeda-dev"
else
  DEPLOYNAME="Andromeda-v$1"
fi
FRAMEWORK="net5.0"

echo "Start publishing as single file executables."
(
  cd "src/Andromeda/AvaloniaApp" || exit

  # Restore
  dotnet tool restore
  dotnet paket restore

  echo "Build for Linux."
  if [ $1 = 'dev' ]
  then
    # We don't need the time consuming trimming so much for continous deployment
    dotnet publish -v m -c Release -r linux-x64 -o "../../../deploy" \
      -p:PublishSingleFile=true \
      -p:IncludeNativeLibrariesForSelfExtract=true \
      -p:DebugType=None
  else
    dotnet publish -v m -c Release -r linux-x64 -o "../../../deploy" \
      -p:PublishSingleFile=true \
      -p:PublishTrimmed=true \
      -p:TrimMode=Link \
      -p:IncludeNativeLibrariesForSelfExtract=true \
      -p:DebugType=None
  fi
  mv "../../../deploy/Andromeda.AvaloniaApp" "../../../deploy/$DEPLOYNAME-linux-x64"

  echo "Build for Windows."
  if [ $1 = 'dev' ]
  then
    # We don't need the time consuming trimming so much for continous deployment
    dotnet publish -v m -c Release -r win-x64 -o "../../../deploy" \
      -p:PublishSingleFile=true \
      -p:IncludeNativeLibrariesForSelfExtract=true \
      -p:DebugType=None
  else
    dotnet publish -v m -c Release -r win-x64 -o "../../../deploy" \
      -p:PublishSingleFile=true \
      -p:PublishTrimmed=true \
      -p:TrimMode=Link \
      -p:IncludeNativeLibrariesForSelfExtract=true \
      -p:DebugType=None
  fi
  mv "../../../deploy/Andromeda.AvaloniaApp.exe" "../../../deploy/$DEPLOYNAME-win-x64.exe"

  # Don't build for macOS for now, it produces additional files (whyever)
  # echo "Build for macOS."
  # if [ $1 = 'dev' ]
  # then
  #   # We don't need the time consuming trimming so much for continous deployment
  #   dotnet publish -v m -c Release -r osx-x64 -o "../../../deploy" \
  #     -p:PublishSingleFile=true \
  #     -p:IncludeNativeLibrariesForSelfExtract=true \
  #     -p:DebugType=None
  # else
  #   dotnet publish -v m -c Release -r osx-x64 -o "../../../deploy" \
  #     -p:PublishSingleFile=true \
  #     -p:PublishTrimmed=true \
  #     -p:TrimMode=Link \
  #     -p:IncludeNativeLibrariesForSelfExtract=true \
  #     -p:DebugType=None
  # fi
  # mv "../../../deploy/Andromeda.AvaloniaApp" "../../../deploy/$DEPLOYNAME-osx-x64"

  echo "Finished publishing as single file executables."
)

echo "Start publishing as AppImage."
(
  cd "src/Andromeda/AvaloniaApp" || exit
  if [ $1 = 'dev' ]
  then
    # We don't need the time consuming trimming so much for continous deployment
    dotnet publish -v m -c Release -f "$FRAMEWORK" -r ubuntu.16.04-x64 \
      -p:DebugType=None
  else
    dotnet publish -v m -c Release -f "$FRAMEWORK" -r ubuntu.16.04-x64 \
      -p:PublishTrimmed=true \
      -p:TrimMode=Link \
      -p:DebugType=None
  fi
  mkdir -p "AppDir/usr"
  mv -T "bin/Release/$FRAMEWORK/ubuntu.16.04-x64/publish" "AppDir/usr/bin"

  cp -a "../../../assets/build/appimage/." "AppDir"
)
if ! [ -f "./appimagetool-x86_64.AppImage" ]
then
  wget "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
  chmod a+x ./appimagetool-x86_64.AppImage
fi
./appimagetool-x86_64.AppImage --appimage-extract
(
  cd "src/Andromeda/AvaloniaApp" || exit
  ./../../../squashfs-root/AppRun --no-appstream ./AppDir -u "gh-releases-zsync|NicoVIII|Andromeda-for-GOG|latest|Andromeda-*.AppImage.zsync"
  mv "Andromeda-x86_64.AppImage" "../../../deploy/Andromeda-x86_64.AppImage"
  mv "Andromeda-x86_64.AppImage.zsync" "../../../deploy/Andromeda-x86_64.AppImage.zsync"
  rm -r "AppDir"

  echo "Finished publishing as AppImage."
)
rm -r squashfs-root
