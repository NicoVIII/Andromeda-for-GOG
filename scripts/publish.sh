#!/bin/bash
echo "Clear deploy folder."
rm -fr "deploy"
mkdir -p "deploy"

DEPLOYNAME="Andromeda-v$1"
FRAMEWORK="netcoreapp3.1"

echo "Start publishing as single file executables."
(
  cd "src/Andromeda/AvaloniaApp.FSharp" || exit

  echo "Build for Windows."
  dotnet publish -v quiet -c Release -r win-x64 -o "../../../deploy/win" -p:PublishSingleFile=true | grep error --color=never

  echo "Build for macOS."
  dotnet publish -v quiet -c Release -r osx-x64 -o "../../../deploy/mac" -p:PublishSingleFile=true | grep error --color=never

  echo "Build for Linux."
  dotnet publish -v quiet -c Release -r linux-x64 -o "../../../deploy/lin" -p:PublishSingleFile=true | grep error --color=never

  (
    cd "../../.." || exit

    echo "Prepare archives"
    cp "LICENSE" "deploy/win"
    cp "LICENSE" "deploy/mac"
    cp "LICENSE" "deploy/lin"
    cp "README.md" "deploy/win"
    cp "README.md" "deploy/mac"
    cp "README.md" "deploy/lin"
    cp "CHANGELOG.md" "deploy/win"
    cp "CHANGELOG.md" "deploy/mac"
    cp "CHANGELOG.md" "deploy/lin"

    (
      cd "deploy/win" || exit
      find . -print | zip -q "../win" -@
      mv "../win.zip" "../$DEPLOYNAME-win-x64.zip"
    )

    (
      cd "deploy/mac" || exit
      tar -czf "../$DEPLOYNAME-mac-x64.tar" -- *
      xz -e9 --threads=0 -f "../$DEPLOYNAME-mac-x64.tar"
    )

    (
      cd "deploy/lin" || exit
      tar -czf "../$DEPLOYNAME-linux-x64.tar" -- *
      xz -e9 --threads=0 -f "../$DEPLOYNAME-linux-x64.tar"
    )

    echo "Delete temporary folders."

    rm -r "deploy/win"
    rm -r "deploy/mac"
    rm -r "deploy/lin"

    echo "Finished publishing as single file executables."
  )
)

echo "Start publishing as AppImage."
(
  cd "src/Andromeda/AvaloniaApp.FSharp" || exit
  dotnet publish --verbosity quiet --configuration Release --framework "$FRAMEWORK" --runtime ubuntu.16.04-x64 | grep error --color=never
  mkdir -p "AppDir/usr"
  mv -T "bin/Release/$FRAMEWORK/ubuntu.16.04-x64/publish" "AppDir/usr/bin"

  cp -a "../../../assets/build/appimage/." "AppDir"
  cp "../../../LICENSE" "AppDir"
  cp "../../../README.md" "AppDir"
  cp "../../../CHANGELOG.md" "AppDir"
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
  mv "Andromeda-x86_64.AppImage" "../../../deploy/$DEPLOYNAME-x86_64.AppImage"
  mv "Andromeda-x86_64.AppImage.zsync" "../../../deploy/$DEPLOYNAME-x86_64.AppImage.zsync"
  rm -r "AppDir"

  echo "Finished publishing as AppImage."
)
rm -r squashfs-root
