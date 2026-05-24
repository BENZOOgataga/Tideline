// Tideline Stream Deck plugin (SDK 6, Node runtime).
//
// On key press, spawn the shipped tideline-capture.exe helper, which writes
// {"cmd":"capture"} to the resident app's named pipe \\.\pipe\tideline. The
// plugin holds no state and no database access; it only signals the app,
// preserving the single-writer rule from SPEC section 16.

const path = require('node:path');
const { spawn } = require('node:child_process');
const WebSocket = require('ws');

const args = parseArgs(process.argv.slice(2));
const port = Number(args.port);
const pluginUUID = args.pluginUUID;
const registerEvent = args.registerEvent;

if (!port || !pluginUUID || !registerEvent) {
  console.error('[Tideline] missing required launch args');
  process.exit(1);
}

const ws = new WebSocket('ws://127.0.0.1:' + port);

ws.on('open', () => {
  ws.send(JSON.stringify({ event: registerEvent, uuid: pluginUUID }));
  console.log('[Tideline] registered');
});

ws.on('message', (raw) => {
  let msg;
  try { msg = JSON.parse(raw.toString()); }
  catch (err) {
    console.error('[Tideline] bad payload', err);
    return;
  }
  if (msg.event === 'keyDown') {
    const settings = (msg.payload && msg.payload.settings) || {};
    triggerCapture(msg.context, settings);
  }
});

ws.on('error', (err) => {
  console.error('[Tideline] socket error', err);
});

ws.on('close', () => {
  // Stream Deck closes the socket when the plugin is being torn down.
  process.exit(0);
});

function triggerCapture(context, settings) {
  const exe = (settings.captureExePath && settings.captureExePath.trim())
    || defaultExePath();
  const captureText = settings.captureText && String(settings.captureText).trim();
  const spawnArgs = captureText ? ['--text', captureText] : [];

  try {
    const child = spawn(exe, spawnArgs, { detached: true, stdio: 'ignore' });
    child.on('error', (err) => {
      console.error('[Tideline] spawn error', err);
      showAlert(context);
    });
    child.unref();
  } catch (err) {
    console.error('[Tideline] spawn threw', err);
    showAlert(context);
  }
}

function showAlert(context) {
  try { ws.send(JSON.stringify({ event: 'showAlert', context })); }
  catch { /* socket already gone */ }
}

function defaultExePath() {
  // Standard Velopack install location for Tideline. Per-user, no admin.
  const localAppData = process.env.LOCALAPPDATA || '';
  if (localAppData) {
    return path.join(localAppData, 'Tideline', 'current', 'tideline-capture.exe');
  }
  return 'tideline-capture.exe';
}

function parseArgs(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i++) {
    const k = argv[i];
    if (k && k.startsWith('-')) {
      out[k.replace(/^-+/, '')] = argv[i + 1];
      i++;
    }
  }
  return out;
}
