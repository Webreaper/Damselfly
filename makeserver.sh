
if [ -n "$1" ]; then
  version=$1;
else
    # Read the version from disk.
    version=`cat VERSION`
fi

echo "**************************** Building Non-Docker Damselfly ****************************"

dotnet publish Damselfly.Web -r osx-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish Damselfly.Web -r linux-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish Damselfly.Web -r win-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true

mkdir server
rm server/*.*

pushd .
cd Damselfly.Web/bin/Release/net5.0/osx-x64/publish
zip -r ../../../../../../server/damselfly-server-mac-$version.zip *.*
popd 

pushd .
cd Damselfly.Web/bin/Release/net5.0/win-x64/publish
zip -r ../../../../../../server/damselfly-server-windows-$version.zip *.*
popd

pushd .
cd Damselfly.Web/bin/Release/net5.0/linux-x64/publish
zip -r ../../../../../../server/damselfly-server-linux-$version.zip *.* 
popd

echo "Non-docker Damselfly build complete."
