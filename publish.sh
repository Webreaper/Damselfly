
if [ -n "$1" ]; then
  version = $1;
else
    # Read the version from disk.
    version=`cat VERSION`
fi

dotnet publish Damselfly.Web -r osx.10.11-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:PublishReadyToRun=true /p:Version=$version

dotnet publish Damselfly.Web -r linux-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version
dotnet publish Damselfly.Web -r ubuntu.18.04-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version
dotnet publish Damselfly.Web -r win-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version

