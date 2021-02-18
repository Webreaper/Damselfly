# Read the version from disk.
version=`cat VERSION`

if [ -n "$1" ]; then
    PLATFORM=$1
else
    echo 'No platform specified. Defaulting to mac.'
    PLATFORM='mac'
fi

case $PLATFORM in
  m1)
    # No M1 runtime for .Net yet.
    runtime='osx-arm64' 
    ;;
  mac)
    runtime='osx-x64'
    ;;
  windows)
    runtime='win-x64'
    ;;
  alpine)
    runtime='alpine-x64'
    ;;
  linux)
    runtime='linux-x64'
    ;;
esac

serverdist="${PWD}/server"
zipname="${serverdist}/damselfly-server-${PLATFORM}-${version}.zip"

echo "*** Building Server for ${PLATFORM} with runtime ${runtime} into ${zipname}"

dotnet publish Damselfly.Web -r $runtime -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true

outputdir="Damselfly.Web/bin/Release/net5.0/${runtime}/publish"

if [ -d "$outputdir" ]; then
  echo "Zipping build to ${zipname}..."
  mkdir $serverdist

  cd $outputdir
  zip $zipname . -rx "*.pdb" 
  echo "Build complete."
else
  echo "ERROR: Output folder ${outputdir} did not exist."
fi

