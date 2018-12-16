#!/bin/bash
echo "Start publishing by building:"
dotnet restore
(
  cd Andromeda.AvaloniaApp || exit
  dotnet publish -c Release
)

rm -fr "deploy"
mkdir -p "deploy"

(
  cd "deploy" || exit
  # Move files to publish folder
  mkdir "publish"
  mv "../Andromeda.AvaloniaApp/bin/Release/netcoreapp2.1/publish" "publish/bin"
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

    echo "Finished publishing."
  )
)
