# Tideline Stream Deck plugin

A small Stream Deck plugin that triggers the Tideline capture overlay over
the local named pipe (`\\.\pipe\tideline`).

## Status of the bundled plugin

**The bundled `Tideline.streamDeckPlugin` does not work as-is in v0.1.0.**

It was scaffolded against the legacy Stream Deck SDK v2 model where the
plugin runs in a sandboxed Chromium webview. That model has no Node
access, so `plugin.js` cannot `require('child_process')` to spawn the
`tideline-capture` helper. The bundle still ships in the release for
inspection and as a starting point for the rewrite.

The real fix is to port the plugin to **Stream Deck SDK 6** (Node-based
runtime), so `plugin.js` runs in a real Node process that can spawn
helpers. Tracked in [OPEN_QUESTIONS.md](../../OPEN_QUESTIONS.md).

## Quick workaround (works today, no custom plugin)

Stream Deck has a built-in **System -> Open** action that runs an
executable on press. Bind it to the bundled `tideline-capture.exe`:

1. Open the Stream Deck app.
2. Drag the **System -> Open** action onto a button.
3. Set the App / File path to the helper exe, for example
   `C:\Users\<you>\AppData\Local\Tideline\current\tideline-capture.exe`.
4. Press the button. The helper writes `{"cmd":"capture"}` to
   `\\.\pipe\tideline`, the resident Tideline raises the capture
   overlay, and you type.

Pass arguments via the same action to script richer captures, for
example:

```
tideline-capture.exe --text "remind me later"
```

`tideline-capture.exe --show` brings the main window to the front.
`tideline-capture.exe --count` prints `{"count":N}` (useful for
diagnostics).

## How the helper works

The plugin (or any other launcher) calls `tideline-capture.exe`, which
connects to the named pipe owned by the running Tideline app and writes
one line of JSON. Tideline then raises the capture overlay through the
same code path as the global Ctrl+Alt+N hotkey. The helper never
touches the database, in line with the single-writer rule in
SPEC section 16.

## Build the helper

```powershell
dotnet publish tools/Tideline.CaptureClient -c Release
```

The `tideline-capture.exe` binary lands in
`tools/Tideline.CaptureClient/bin/Release/net8.0/win-x64/publish/`.
The release workflow also publishes it and copies it into the
`.streamDeckPlugin` bundle's `bin/` folder.

## Assets

`tidelineIcon.png` (144 px) and `tidelineIcon@2x.png` (288 px) are
committed under `com.tideline.capture.sdPlugin/`, derived from the
shared brand artwork. Stream Deck picks them up automatically.
