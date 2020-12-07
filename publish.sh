
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
rm damselfly-for-mac.zip
rm damselfly-for-windows.zip
rm damselfly-for-linux.zip

pushd .
cd Damselfly.Web/bin/Release/net5.0/osx-x64/publish
zip -r ../../../../../../server/damselfly-for-mac.zip *.*
popd 

pushd .
cd Damselfly.Web/bin/Release/net5.0/win-x64/publish
zip -r ../../../../../../server/damselfly-for-windows.zip *.*
popd

pushd .
cd Damselfly.Web/bin/Release/net5.0/linux-x64/publish
zip -r ../../../../../../server/damselfly-for-linux.zip *.* 
popd

echo "Non-docker Damselfly build complete."
