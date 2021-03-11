
# Valid platforms are 'mac', 'windows' and 'linux'
if [ -n "$1" ]; then
    PLATFORM=$1
else
    echo 'No platform specified. Defaulting to mac.'
    PLATFORM='mac'
fi

echo "Building Damselfly for ${PLATFORM}..."

# bump version
docker run --rm -v "$PWD":/app treeder/bump $RELEASE
version=`cat VERSION`
echo "\nGenerating new version: $version"

rm Damselfly.Web/wwwroot/desktop/*.*
rm server/*.*

# Make the desktop Electron app (now done in the dockerfile)
sh scripts/makedesktop.sh $PLATFORM

# Create the non-docker versions
sh scripts/makeserver.sh $PLATFORM

# Create the docker versions
sh scripts/makedocker.sh $PLATFORM

echo "Damselfly build complete."
