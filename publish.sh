
if [ -n "$1" ]; then
  version = $1;
else
    # Read the version from disk.
    version=`cat VERSION`
fi

dotnet publish Damselfly.Web -r osx-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version
dotnet publish Damselfly.Web -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version
dotnet publish Damselfly.Web -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version

