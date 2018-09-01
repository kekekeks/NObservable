#!/bin/bash
set -e
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
if [ $# -eq 0 ]
then
  echo "No arguments supplied"
  exit 1
fi
VERSION=$1
cd $DIR
set -x
msbuild /t:Restore /p:Version=$VERSION
rm -rf build-packages
mkdir build-packages
rm -rf src/*/bin/Release

VARGS="/p:Version=$VERSION /p:Configuration=Release"
MSB="msbuild $VARGS"

$MSB /t:Clean
for p in NObservable NObservable.Fody NObservable.Blazor
do
	cd $DIR/src/$p
	$MSB /t:Build
done

for p in NObservable.Fody NObservable NObservable.Blazor
do
	cd $DIR/src/$p
	$MSB /t:Pack
	cp bin/Release/$p.*.nupkg $DIR/build-packages
done

