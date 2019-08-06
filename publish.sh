#!/bin/bash
echo "Clear deploy folder."
rm -fr "deploy"
mkdir -p "deploy"

DEPLOYNAME="Andromeda-v0.0.0-x86_64"

echo "Start publishing as archive by building."
(
  cd src/Andromeda.AvaloniaApp || exit
  dotnet restore -s "https://www.myget.org/F/avalonia-ci/api/v2" -s "https://api.nuget.org/v3/index.json"
  dotnet publish --verbosity quiet --configuration Release --framework netcoreapp2.2 --no-restore | grep error --color=never
)

(
  cd "deploy" || exit
  # Move files to publish folder
  mkdir "publish"
  mv "../src/Andromeda.AvaloniaApp/bin/Release/netcoreapp2.2/publish" "publish/bin"
  cp "../build/start.cmd" "publish"
  cp "../build/start.sh" "publish"
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
  cd "src/Andromeda.AvaloniaApp" || exit
  dotnet restore --runtime ubuntu.16.04-x64 -s "https://www.myget.org/F/avalonia-ci/api/v2" -s "https://api.nuget.org/v3/index.json"
  dotnet publish --verbosity quiet --configuration Release --framework netcoreapp2.2 --runtime ubuntu.16.04-x64 | grep error --color=never
  rm -r "AppDir/usr/bin"
  mv "bin/Release/netcoreapp2.2/ubuntu.16.04-x64/publish" "AppDir/usr"
  mv "AppDir/usr/publish" "AppDir/usr/bin"
)
if ! [ -f "./appimagetool-x86_64.AppImage" ]
then
  wget "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
  chmod a+x appimagetool-x86_64.AppImage
fi
(
  cd "src/Andromeda.AvaloniaApp" || exit
  ./../appimagetool-x86_64.AppImage ./AppDir -u "gh-releases-zsync|NicoVIII|Andromeda-for-GOG|latest|Andromeda-*.AppImage.zsync"
  mv "Andromeda-x86_64.AppImage" "../deploy/$DEPLOYNAME.AppImage"
  mv "Andromeda-x86_64.AppImage.zsync" "../deploy/$DEPLOYNAME.AppImage.zsync"

  echo "Finished publishing as AppImage."
)
