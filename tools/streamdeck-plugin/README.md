# Tideline Stream Deck plugin

A small Stream Deck plugin that triggers the Tideline capture overlay over
the local named pipe (`\\.\pipe\tideline`).

## How it works

The plugin runs in the Stream Deck runtime and, on key press, spawns the
`tideline-capture.exe` helper (built from
[`tools/Tideline.CaptureClient/`](../Tideline.CaptureClient/)).
The helper writes a single line of JSON to the named pipe owned by the
running Tideline app, which then raises the capture overlay through the
same code path as the global hotkey. The plugin itself never touches the
database, in line with the single-writer rule in SPEC section 16.

## Build the helper

```powershell
dotnet publish tools/Tideline.CaptureClient -c Release
```

The `tideline-capture.exe` binary lands in
`tools/Tideline.CaptureClient/bin/Release/net8.0/win-x64/publish/`.

## Bundle the plugin

A `.streamDeckPlugin` file is a zip of the `com.tideline.capture.sdPlugin`
folder, renamed to `.streamDeckPlugin`. From this folder:

```powershell
Compress-Archive -Path com.tideline.capture.sdPlugin -DestinationPath Tideline.streamDeckPlugin.zip -Force
Rename-Item Tideline.streamDeckPlugin.zip Tideline.streamDeckPlugin
```

Double-click `Tideline.streamDeckPlugin` to install it into the Stream
Deck app. In the action's property inspector, point "Path to
tideline-capture.exe" at the helper you built above.

## Open questions

- An icon set (`tidelineIcon.png` at 1x, 2x, 3x) is not yet committed.
  The Stream Deck app substitutes a default if the asset is missing.
  See [OPEN_QUESTIONS.md](../../OPEN_QUESTIONS.md).
