#!/bin/bash
echo "Clear deploy folder."
rm -fr "deploy"
mkdir -p "deploy"

echo "Start publishing as archive by building."
dotnet restore
(
  cd Andromeda.AvaloniaApp || exit
  dotnet publish --verbosity quiet --configuration Release --framework netcoreapp2.2 | grep error --color=never
)

(
  cd "deploy" || exit
  # Move files to publish folder
  mkdir "publish"
  mv "../Andromeda.AvaloniaApp/bin/Release/netcoreapp2.2/publish" "publish/bin"
  cp "../build/start.cmd" "publish"
  cp "../build/start.sh" "publish"
  cp "../LICENSE" "publish"
  cp "../README.md" "publish"
  cp "../CHANGELOG.md" "publish"
  (
    cd "publish" || exit
    echo "Put files into .zip .."
    find . -print | zip -q "../publish" -@

    echo "Put files into .tar .."
    tar -czf "../publish.tar" -- *
    echo "Compress .tar to .tar.gz .."
    gzip -k "../publish.tar"
    echo "Compress .tar to .tar.xz .."
    xz -e9 --threads=0 -f "../publish.tar"

    echo "Finished publishing as archives."
  )
)

echo "Start publishing as AppImage by building."
(
  cd "Andromeda.AvaloniaApp"
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
  cd "Andromeda.AvaloniaApp"
  ./../appimagetool-x86_64.AppImage ./AppDir
  mv "Andromeda-x86_64.AppImage" "../deploy"

  echo "Finished publishing as archives."
)
