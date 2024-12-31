# Read the version from disk.
version=`cat VERSION`
dotnetversion='9.0'

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
  linux)
    runtime='linux-x64'
    ;;
esac

serverdist="${PWD}/server"
zipname="${serverdist}/damselfly-server-${PLATFORM}-${version}.zip"
project='Damselfly.Web.Server'
outputdir="$project/bin/Release/net${dotnetversion}/${runtime}/publish"

echo "*** Building Damselfly for ${PLATFORM} with runtime ${runtime}"

# dotnet publish $project -r $runtime -f net${dotnetversion} -c Release --self-contained true /p:Version=$version 
#  /p:PublishTrimmed=true /p:EnableCompressionInSingleFile= /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile= /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish $project -r $runtime -f net${dotnetversion} -c Release /p:Version=$version 

if [ $? -ne 0 ]; then
  echo "*** ERROR: Dotnet Build failed. Exiting."
  exit 1
fi

echo "*** ${project} build succeeded. Packaging..."

if [ -d "$outputdir" ]; then
  echo "*** Contents of ${outputdir}:"

  ls -l $outputdir

  echo "*** Zipping build to ${zipname}..."
  mkdir $serverdist

  cd $outputdir
  zip $zipname . -rx "*.pdb" 
  echo "*** Build complete."
else
  echo "*** ERROR: Output folder ${outputdir} did not exist."
  exit 1
fi

