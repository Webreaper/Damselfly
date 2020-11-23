cd Damselfly.Desktop
npm install
npm version $1
rm -rf ./dist
yarn dist
mkdir -p ../Damselfly.Web/wwwroot/desktop
cd dist 
rm ../../Damselfly.Web/wwwroot/desktop/damselfly-macos.zip
zip ../../Damselfly.Web/wwwroot/desktop/damselfly-macos.zip *.dmg
