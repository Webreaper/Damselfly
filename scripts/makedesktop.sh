
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
    copyfiles="dist/*.zip"
    ;;
  windows)
    yarncommand='distwin'
    copyfiles="dist/*.zip"
    ;;
  linux)
    yarncommand='distlinux'
    copyfiles="dist/*.AppImage"
    ;;
esac

destfolder="${PWD}/Damselfly.Web/wwwroot/desktop"

cd Damselfly.Desktop

echo "*** Building Electron Desktop app for ${PLATFORM}..."

yarn 
yarn install 
yarn version --new-version $version 
yarn $yarncommand

echo "Desktop build complete. Copying ${copyfiles} to ${destfolder}..."

mkdir -p $destfolder
cp $copyfiles $destfolder

echo "Desktop build complete"