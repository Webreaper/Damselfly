# Read the version from disk.
version=`cat VERSION`

echo Building Server for $1 into $2-$version.zip

dotnet publish Damselfly.Web -r $1 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:Version=$version /p:IncludeNativeLibrariesForSelfExtract=true

cd $GITHUB_WORKSPACE/Damselfly.Web/bin/Release/net5.0/$1/publish
mkdir $GITHUB_WORKSPACE/server
zip -r $GITHUB_WORKSPACE/server/$2-$version.zip *.* -x "*.pdb" 

