
# Read the version from disk.
version=`cat VERSION`

if [ -n "$1" ]; then
    PLATFORM=$1
else
    echo 'No platform specified. Defaulting to mac.'
    PLATFORM='mac'
fi

yarncommand="dist${PLATFORM}"
destfolder="${PWD}/Damselfly.Web/wwwroot/desktop"

cd Damselfly.Desktop

echo "*** Building Electron Desktop app for ${PLATFORM}..."

yarn 
yarn install 
yarn version --new-version $version 
yarn $yarncommand

echo "Desktop build complete. Copying to ${destfolder}..."

mkdir -p $destfolder
cp dist/*.zip $destfolder
cp dist/*.AppImage $destfolder

