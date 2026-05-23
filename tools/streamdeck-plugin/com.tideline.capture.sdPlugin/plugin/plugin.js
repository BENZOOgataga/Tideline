// Tideline Stream Deck plugin.
// Connects to the Stream Deck SDK websocket and, on key press, asks the
// resident Tideline app to open the capture overlay by writing to the
// named pipe \\.\pipe\tideline. The plugin holds no state and no database
// access; it only signals the app, preserving the single-writer rule
// described in SPEC section 16.

let ws = null;
let pluginUUID = null;

function connectElgatoStreamDeckSocket(inPort, inPluginUUID, inRegisterEvent, inInfo) {
  pluginUUID = inPluginUUID;
  ws = new WebSocket('ws://127.0.0.1:' + inPort);
  ws.onopen = function () {
    const registerJson = { event: inRegisterEvent, uuid: inPluginUUID };
    ws.send(JSON.stringify(registerJson));
  };
  ws.onmessage = function (evt) {
    const message = JSON.parse(evt.data);
    if (message.event === 'keyDown') {
      triggerCapture(message.payload && message.payload.settings);
    }
  };
}

function triggerCapture(settings) {
  try {
    // The Stream Deck SDK runs plugins in an Electron-like context with
    // limited Node access. We rely on the small "tideline-capture" helper
    // that ships next to Tideline.exe. The path is read from per-action
    // settings, falling back to PATH lookup.
    const exePath = (settings && settings.captureExePath) || 'tideline-capture.exe';
    const args = settings && settings.captureText ? ['--text', settings.captureText] : [];
    // Node's child_process is exposed in the Stream Deck plugin runtime.
    const child_process = require('child_process');
    child_process.spawn(exePath, args, { detached: true, stdio: 'ignore' }).unref();
  } catch (err) {
    showAlert();
  }
}

function showAlert() {
  if (!ws || !pluginUUID) return;
  ws.send(JSON.stringify({ event: 'showAlert', context: pluginUUID }));
}
