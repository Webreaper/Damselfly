
if [ -n "$1" ]; then
    version=$1;
else
    # Read the version from disk.
    version=`cat VERSION`
fi

cd Damselfly.Desktop

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

echo "Copying Mac, Windows and Linux desktop apps to Damselfly.Web/wwwroot/desktop..."
cp dist/Damselfly-$version-mac.zip ../Damselfly.Web/wwwroot/desktop/damselfly-mac.zip
cp dist/Damselfly-$version-win.zip ../Damselfly.Web/wwwroot/desktop/damselfly-win.zip
cp dist/Damselfly-$version.AppImage ../Damselfly.Web/wwwroot/desktop/damselfly-linux.appimage

echo "Desktop build complete."
