#!/bin/bash
echo "Clear deploy folder."
rm -fr "deploy"
mkdir -p "deploy"

DEPLOYNAME="Andromeda-v0.0.0-x86_64"
FRAMEWORK="netcoreapp3.0"

echo "Start publishing as archive by building."
(
  cd src/Andromeda/AvaloniaApp.FSharp || exit
  dotnet publish --verbosity quiet --configuration Release --framework "$FRAMEWORK" | grep error --color=never
)

(
  cd "deploy" || exit
  # Move files to publish folder
  mkdir "publish"
  mv "../src/Andromeda/AvaloniaApp.FSharp/bin/Release/$FRAMEWORK/publish" "publish/bin"
  cp -a "../assets/build/archive/." "publish"
  cp "../LICENSE" "publish"
  cp "../README.md" "publish"
  cp "../CHANGELOG.md" "publish"
  (
    cd "publish" || exit
    echo "Put files into .zip .."
    find . -print | zip -q "../publish" -@
    mv "../publish.zip" "../$DEPLOYNAME.zip"

    echo "Put files into .tar .."
    tar -czf "../$DEPLOYNAME.tar" -- *
    echo "Compress .tar to .tar.gz .."
    gzip -k "../$DEPLOYNAME.tar"
    echo "Compress .tar to .tar.xz .."
    xz -e9 --threads=0 -f "../$DEPLOYNAME.tar"

    echo "Finished publishing as archives."
  )
)

echo "Start publishing as AppImage by building."
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
  mv "Andromeda-x86_64.AppImage" "../../../deploy/$DEPLOYNAME.AppImage"
  mv "Andromeda-x86_64.AppImage.zsync" "../../../deploy/$DEPLOYNAME.AppImage.zsync"
  rm -r "AppDir"

  echo "Finished publishing as AppImage."
)
rm -r squashfs-root
