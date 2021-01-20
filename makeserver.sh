
if [ -n "$1" ]; then
  version=$1;
else
    # Read the version from disk.
    version=`cat VERSION`
fi

echo "**************************** Building Non-Docker Damselfly ****************************"

mkdir server

echo Building OSX Server...
dotnet publish Damselfly.Web -r osx-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true

echo Building Linux Server...
dotnet publish Damselfly.Web -r linux-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true

echo Building Windows Server...
dotnet publish Damselfly.Web -r win-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true

echo Zipping OSX Server...
cd Damselfly.Web/bin/Release/net5.0/osx-x64/publish
zip -r ../../../../../../server/damselfly-server-mac-$version.zip *.* -x ".pdb" 
cd ../../../../../..

echo Zipping Windows Server...
cd Damselfly.Web/bin/Release/net5.0/win-x64/publish
zip -r ../../../../../../server/damselfly-server-windows-$version.zip *.* -x ".pdb" 
cd ../../../../../..

echo Zipping Linux Server...
cd Damselfly.Web/bin/Release/net5.0/linux-x64/publish
zip -r ../../../../../../server/damselfly-server-linux-$version.zip *.* -x ".pdb" 
cd ../../../../../..

pwd
ls server

echo "Non-docker Damselfly build complete."
