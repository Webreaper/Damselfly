const { remote, ipcRenderer } = require('electron');
const url = require("url");

let configData;

// Initialise this property with the desktop version
window.desktopVersion = ipcRenderer.sendSync('get-version');

// Wrapper method to download a file. Pass the URL of the
// file to download, and the relative path to the destination
// file. These then get passed through to the Main method
// via IPC, where the work is done.
window.downloadUrl = function (urlStr, destFile) {
  data = { url: urlStr, dest: destFile };
  console.log('Saving file to ' + destFile);
  ipcRenderer.send('save-local', data);
}

// Initialise the config by sending a synchronous message to
// the main process, which will return the current configData
initConfig = function () {
  console.log('Initialising...');
  configData = ipcRenderer.sendSync('config', 'page');
  console.log('Url: ' + configData.url);
  return configData;
}

// Helper function to validate that a string is a valid URL. 
// This is used by the settings form
invalidUrl = function (s) 
{
  if (s === "")
    return true;
  try {
    new URL( s );
    return false;
  } 
  catch (err) 
  {
     console.log( "Damselfly url " + s + " is invalid: " + err + " (URL = " + URL + ")");
    return true;
  }
};

// When the form is submitted, we set the config by posting
// it through the IPC to the main process, which will do the
// actual persistence to disk. Then we redirect to the URL
// specified in the settings.
saveConfig = function (urlStr, localfolder) {
  data = { url: urlStr, folder: localfolder };
  ipcRenderer.send('set-config', data);
  // Now, we've saved it, so go there.
  location.href = data.url;
}

// Listen for the server-version message. When we get it, we
// return the version of the server
ipcRenderer.on('check-server-version', (event,args) => {
  console.log( 'Getting server version ');
  
    try{
      if( typeof window.checkDesktopUpgrade === 'function')
      {
        setTimeout( function() {
          try
          {
            // Call the server-provided method to check the version
            // passing in the version we're currently running.
            window.checkDesktopUpgrade(window.desktopVersion, function( upgradeVersion ) {
                  msg = 'There is a new version of the Damselfly Desktop client available (' + upgradeVersion + ').';
                  msg = msg + '\nPlease download the latest version from the About page.';    
                  alert( msg) ;
             });
          }
          catch( err )
          {
            console.log( 'Unable to check the server version: ' + err);
          }
          }, 3000 );
      }
    }
    catch( err )
    {
      console.log( 'Unable to check the server version: ' + err);
    }
});
