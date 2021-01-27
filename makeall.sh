
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

mkdir publish
cd publish
rm *.*
unzip -o ../server/*.zip
cd ..

# Create the docker versions. Pass 'master' here to build a real version
sh scripts/makedocker.sh 

echo "Damselfly build complete."
