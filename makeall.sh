
RELEASE='patch'
if [ "$1" == "release" ]; then
    RELEASE='minor'
fi

echo Creating $RELEASE release.
echo "Don't forget to run EF migrations..."

# bump version
docker run --rm -v "$PWD":/app treeder/bump $RELEASE
version=`cat VERSION`
echo "\nGenerating new version: $version"

# Make the desktop Electron app (now done in the dockerfile)
sh makedesktop.sh $version

# Create the non-docker versions
sh makeserver.sh $version

# Create the docker versions
sh makedocker.sh $version

echo "Damselfly build complete."
