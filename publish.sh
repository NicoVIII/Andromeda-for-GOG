#!/bin/bash
echo "Start publishing by building:"
dotnet restore
cd Andromeda.ConsoleApp
  dotnet fake build --target Publish
  cd ..

rm -fr "deploy"
mkdir -p "deploy"

cd "deploy"
  # Move files to publish folder
  mkdir "publish"
  mv "../Andromeda.ConsoleApp/bin/Release/netcoreapp2.1/publish" "publish/bin"
  cp "../build/start.cmd" "publish"
  cp "../build/start.sh" "publish"

  cd "publish"
    echo "Put files into .zip .."
    find . -print | zip -q "../publish" -@

    echo "Put files into .tar .."
    tar -czf "../publish.tar" *
    echo "Compress .tar to .tar.gz .."
    gzip -k "../publish.tar"
    echo "Compress .tar to .tar.xz .."
    xz -e9 --threads=0 -f "../publish.tar"

    echo "Finished publishing."
    cd ..
  cd ..
