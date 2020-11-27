
if [ -n "$1" ]; then
    version=$1;
else
    # Read the version from disk.
    version=`cat VERSION`
fi

cd Damselfly.Desktop
npm install
npm version $version
rm -rf ./dist
yarn distmac
mkdir -p ../Damselfly.Web/wwwroot/desktop
cd dist 
rm ../../Damselfly.Web/wwwroot/desktop/damselfly-macos.zip
zip ../../Damselfly.Web/wwwroot/desktop/damselfly-macos.zip *.dmg
