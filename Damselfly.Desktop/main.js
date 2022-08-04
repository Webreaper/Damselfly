// Modules to control application life and create native browser window
const { app, Menu, BrowserWindow, ipcMain, shell } = require('electron')
const path = require('path')
const fs = require('fs')
const http = require('http');
const log = require('electron-log');
const { AsyncLocalStorage } = require('async_hooks');
const isMac = process.platform === 'darwin'

let mainWindow;

const template = [
  // { role: 'appMenu' }
  ...(isMac ? [{
    label: app.name,
    submenu: [
      { role: 'about' },
      { type: 'separator' },
      { role: 'services' },
      { type: 'separator' },
      { role: 'hide' },
      { role: 'hideothers' },
      { role: 'unhide' },
      { type: 'separator' },
      { role: 'quit' }
    ]
  }] : []),
  // { role: 'fileMenu' }
  {
    label: 'File',
    submenu: [
      {
        label: 'Preferences...',
        click: async () => {
          mainWindow.loadFile(',/index.html');
        }
      },
      isMac ? { role: 'close' } : { role: 'quit' }
    ]
  },
  // { role: 'editMenu' }
  {
    label: 'Edit',
    submenu: [
      { role: 'undo' },
      { role: 'redo' },
      { type: 'separator' },
      { role: 'cut' },
      { role: 'copy' },
      { role: 'paste' },
      ...(isMac ? [
        { role: 'pasteAndMatchStyle' },
        { role: 'delete' },
        { role: 'selectAll' }

      ] : [
          { role: 'delete' },
          { type: 'separator' },
          { role: 'selectAll' }
        ])
    ]
  },
  // { role: 'viewMenu' }
  {
    label: 'View',
    submenu: [
      { role: 'reload' },
      { role: 'forcereload' },
      { role: 'toggledevtools' },
      { type: 'separator' },
      { role: 'resetzoom' },
      { role: 'zoomin' },
      { role: 'zoomout' },
      { type: 'separator' },
      { role: 'togglefullscreen' }
    ]
  },
  // { role: 'windowMenu' }
  {
    label: 'Window',
    submenu: [
      { role: 'minimize' },
      { role: 'zoom' },
      ...(isMac ? [
        { type: 'separator' },
        { role: 'front' },
        { type: 'separator' },
        { role: 'window' }
      ] : [
          { role: 'close' }
        ])
    ]
  },
  {
    role: 'help',
    submenu: [
      {
        label: 'Learn More',
        click: async () => {
          await shell.openExternal('https://damselfly.info')
        }
      }
    ]
  }
]

// Closure class to handle window position persistence. 
// This tracks the window, and whenever it changes, 
// updates the global configData object with the 
// current window position.
function windowStateKeeper(configData) {
  let window, windowState
  function setBounds(initialState) {
    // Restore from appConfig
    if (initialState === null || initialState === undefined) {
      // Default
      windowState = {
        x: undefined,
        y: undefined,
        width: 1000,
        height: 800,
      };
    }
    else {
      windowState = initialState;
    }
  }
  function saveState() {
    if (!windowState.isMaximized) {
      windowState = window.getBounds();
    }
    windowState.isMaximized = window.isMaximized();
    // Store it globally, but don't write to disk yet.
    configData.windowState = windowState;
  }

  function track(win) {
    window = win;
    ['resize', 'move', 'close'].forEach(event => {
      win.on(event, saveState);
    });
  }

  log.info("Initialising windowStateKeeper.");
  setBounds(configData.windowState);

  return ({
    x: windowState.x,
    y: windowState.y,
    width: windowState.width,
    height: windowState.height,
    isMaximized: windowState.isMaximized,
    track,
  });
}

// function to read our app config from a json file
function readConfig(configPath) {
  try {
    log.info('Reading config from ' + configPath);
    configData = JSON.parse(fs.readFileSync(configPath, 'utf8'))
    return configData;
  }
  catch (err) {
    return {
      url: null,
      folder: null,
      windowState: null
    }
  }
}

// Function to download a file from the hosted site/page,
// and save it locally in the file system. The parameters
// are:
// - URL:  something like http://damselfly/rawimage/1234
// - Dest: relative path to save, e.g /Mark Photos/P12345.JPG
// The relative path is joined with the local Pictures 
// folder from the settings/config, and the file is written
// there - unless the file already exists, in which case
// we skip.
function downloadFile(url, dest, cb) {

  // Convert relative path into absolute
  var localPath = path.join(configData.folder, dest);
  var dirname = path.dirname(localPath);
  var filename = path.basename( localPath );

  if (fs.existsSync(localPath)) {
    msg = 'File exists at ' + localPath + ' - skipping copy.';
    log.info(msg);
    // Never ever ever overwrite an existing local file
    return cb(msg);
  }

  // Okay, so let's do it
  log.info('Downloading ' + url + ' to ' + localPath);

  fs.mkdirSync(dirname, { recursive: true });

  var tempFile = localPath + '.tmp';

  // Now open the file for writing
  var dlFile = fs.createWriteStream(tempFile);

  dlFile.on('close', function() {
    log.info( 'File ' + url + ' written to ' + localPath);
    try{
      fs.renameSync( tempFile, localPath);
    }
    catch( err )
    {
      log.error( "Failed to rename temp file: " + tempFile);
    }
  });

  dlFile.on('error', (err) => { // Handle errors
    fs.unlinkSync(tempFile); // Delete the file async. (But we don't check the result) 
    log.error('Error saving file: ' + err.message);
    return cb(err.message);
  });

  var request = http.get(url, (response) => {
    // check if response is success
    if (response.statusCode !== 200) {
      return cb('Response status was ' + response.statusCode);
    }

    response.pipe(dlFile);
  });

  // check for request error too
  request.on('error', (err) => {
    fs.unlinkSync(tempFile);
    log.error('Error saving file: ' + err.message);
    return cb(err.message);
  });
};

// Write the config to the settings file on-disk in the 
// UserData area.
function writeConfig() {
  jsonConfig = JSON.stringify(configData, null, 2);
  log.info('Saving config' + jsonConfig);
  fs.writeFileSync(configPath, jsonConfig, 'utf-8');
}

// Wrapper for loading the settings page 
function loadSettings(window) {
  log.info('Loading preferences page...');
  // Load Window
  window.loadFile('./index.html');
}

// Create the main application window, and do all 
// the work to set up the application. 
function createWindow() {
  log.info('Initialising Window');
  configPath = path.join(app.getPath('userData'), 'damselfly.config');
  configData = readConfig(configPath)

  const winStateKeeper = windowStateKeeper(configData);

  log.info('Creating Damselfly window.');
  // Create the browser window.
  mainWindow = new BrowserWindow({
    x: winStateKeeper.x,
    y: winStateKeeper.y,
    width: winStateKeeper.width,
    height: winStateKeeper.height,
    backgroundColor: black,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: false
    }

  })

  // Set up the IPC handlers for the app

  // First, if the window closes, save our config to disk.
  mainWindow.on('close', function () {
    writeConfig();
    log.info('Window closed.');
  });

  // If the config is set by the settings page, write
  // the config to disk
  ipcMain.on('set-config', (event, data) => {
    log.info('Settings posted from page: ' + data.url);
    configData.url = data.url;
    configData.folder = data.folder;
    writeConfig();
  });

  // If the hosted Damselfly webpage calls the 'save-local' API, 
  // we trigger the save to the local file-system.
  ipcMain.on('save-local', (event, data) => {
    log.info('Saving file to local FS ' + data.dest);
    try{ downloadFile(data.url, data.dest, function(err)
            { log.warn( err )} );
    }
    catch( err )
    {
      log.error( 'Error downloading file: '+ err);
    }
  });

  // When the settings page loads, it will request the current
  // config, so it can pre-populate the settings fields in the
  // form. We return synchronously, passing back the configData.
  ipcMain.on('get-version', function (event, arg) {
    log.info('Desktop version requested from renderer: ' + app.getVersion());
    event.returnValue = app.getVersion();
  });   

  // When the server page loads, we send the 'server-version' 
  // message to the renderer. That will query the server, and 
  // then respond, passing back the server version, via this
  // message.
  ipcMain.on('server-version-reply', function (event, arg) {
    log.info('Version sent back from server:' + arg);
  });   

  // When the settings page loads, it will request the current
  // config, so it can pre-populate the settings fields in the
  // form. We return synchronously, passing back the configData.
  ipcMain.on('config', function (event, arg) {
    log.info('Config requested from renderer:' + arg);
    event.returnValue = configData;
  });    

  // Start tracking the window state
  winStateKeeper.track(mainWindow);

  // Now load our content. If we have a Damselfly URL saved in the 
  // config, then we load that. Otherwise, load the settings page.
  try {
    if (configData.url === null) {
      // and load the index.html of the app.
      loadSettings(mainWindow);
    }
    else {
      mainWindow.loadURL(configData.url)
      mainWindow.webContents.on('did-finish-load', function () {
        log.info( 'Requesting server version...');
        serverVersion = mainWindow.webContents.send('check-server-version');
      });

      mainWindow.webContents.on("did-fail-load", function () {
        // Failed. Default to the settings page
        loadSettings(mainWindow);
      });
    }
  }
  catch (err) {
    log.error('Exception: ' + err);
    loadSettings();
  }

  log.info('Setting up menu.');
  const menu = Menu.buildFromTemplate(template)
  Menu.setApplicationMenu(menu)

  // Open the DevTools.
  //mainWindow.webContents.openDevTools()

  log.info('CreateWindow complete.');
}


// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.whenReady().then(() => {
  createWindow()

  app.on('activate', function () {
    // On macOS it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if (BrowserWindow.getAllWindows().length === 0)
      createWindow()
  })
})

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', function () {
  log.info('Exiting application');
  if (isMac)
    app.quit()
})



