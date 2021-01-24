
# Read the version from disk.
version=`cat VERSION`

if [ -n "$1" ]; then
    PLATFORM=$1
else
    echo 'No platform specified. Defaulting to mac.'
    PLATFORM='mac'
fi

case $PLATFORM in
  mac)
    yarncommand='distmac'
    ;;
  windows)
    yarncommand='distwin'
    ;;
  linux)
    yarncommand='distlinux'
    ;;
esac

destfolder="${PWD}/Damselfly.Web/wwwroot/desktop"

cd Damselfly.Desktop

echo "*** Building Electron Desktop app for ${PLATFORM}..."

yarn 
yarn install 
yarn version --new-version $version 
yarn $yarncommand

echo "Desktop build complete. Copying output to ${destfolder}..."

mkdir -p $destfolder

case $PLATFORM in
  mac)
    cd dist
    zip "${destfolder}/Damselfly-${version}-mac.zip" "Damselfly-${version}.dmg"
    ;;
  windows)
    cp "dist/Damselfly-${version}-win.zip" $destfolder
    ;;
  linux)
    cp "dist/Damselfly-${version}.AppImage" $destfolder
    ;;
esac

echo "Desktop build complete"