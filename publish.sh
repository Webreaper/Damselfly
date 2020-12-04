
if [ -n "$1" ]; then
  version=$1;
else
    # Read the version from disk.
    version=`cat VERSION`
fi

dotnet publish Damselfly.Web -r osx-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish Damselfly.Web -r linux-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish Damselfly.Web -r win-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true

