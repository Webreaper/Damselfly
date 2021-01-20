
if [ -n "$1" ]; then
    version=$1;
else
    # Read the version from disk.
    version=`cat VERSION`
fi

cd Damselfly.Desktop
rm -rf ./dist

echo "**************************** Building Desktop Electron Apps ****************************"

env | grep -iE 'DEBUG|NODE_|ELECTRON_|YARN_|NPM_|CI|CIRCLE|TRAVIS_TAG|TRAVIS|TRAVIS_REPO_|TRAVIS_BUILD_|TRAVIS_BRANCH|TRAVIS_PULL_REQUEST_|APPVEYOR_|CSC_|GH_|GITHUB_|BT_|AWS_|STRIP|BUILD_' > docker_env

docker run --rm  \
 --env-file docker_env \
 --env ELECTRON_CACHE="/root/.cache/electron" \
 --env ELECTRON_BUILDER_CACHE="/root/.cache/electron-builder" \
 -v ${PWD}:/project \
 -v ${PWD##*/}-node-modules:/project/node_modules \
 -v ~/.cache/electron:/root/.cache/electron \
 -v ~/.cache/electron-builder:/root/.cache/electron-builder \
 electronuserland/builder:wine  \
    yarn && yarn install && yarn version --new-version $version && yarn dist

cd dist
ls dist 

mkdir ../../desktop
rm ../../desktop/*.*

echo "Zipping up Mac DMG..."
zip ../../desktop/damselfly-mac.zip Damselfly-$version.dmg

echo "Copying Mac, Windows and Linux desktop apps to /desktop..."
cp Damselfly-$version-win.zip ../../desktop/damselfly-win.zip
cp Damselfly-$version.AppImage ../../desktop/damselfly-linux.appimage

echo "Copying Mac, Windows and Linux desktop apps to Damselfly.Web/wwwroot/desktop..."
rm ../../Damselfly.Web/wwwroot/desktop/*.*
cp ../../desktop/*.* ../../Damselfly.Web/wwwroot/desktop

echo "Desktop build complete."
