function getDesktopVersion() {
    return window.desktopVersion;
}

window.checkDesktopUpgrade = function (desktopVersion, cb) {
    try {
        DotNet.invokeMethodAsync('Damselfly.Web', 'GetUpgradeVersion', desktopVersion).then(upgradeVersion => {
            if (upgradeVersion !== '') {
                cb(upgradeVersion);
            }
        })
    } catch (err) {
        console.log("Error: " + err);
    }
}
