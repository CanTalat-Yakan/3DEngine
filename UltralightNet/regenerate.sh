#!/bin/bash

TFM="net8.0"

# https://stackoverflow.com/questions/59895/getting-the-source-directory-of-a-bash-script-from-within
SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

dotnet build -c Release -f $TFM $DIR/src/UltralightNet
dotnet build -c Release -f $TFM $DIR/src/UltralightNet.AppCore
cp $DIR/src/UltralightNet/obj/Release/$TFM/generated/Microsoft.Interop.LibraryImportGenerator/Microsoft.Interop.LibraryImportGenerator/LibraryImports.g.cs $DIR/src/UltralightNet/LibraryImportGenerator/LibraryImports.g.cs
mkdir -p $DIR/src/UltralightNet.AppCore/LibraryImportGenerator
cp $DIR/src/UltralightNet.AppCore/obj/Release/$TFM/generated/Microsoft.Interop.LibraryImportGenerator/Microsoft.Interop.LibraryImportGenerator/LibraryImports.g.cs $DIR/src/UltralightNet.AppCore/LibraryImportGenerator/LibraryImports.g.cs
