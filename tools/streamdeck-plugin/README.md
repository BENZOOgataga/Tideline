# Tideline Stream Deck plugin

A Stream Deck plugin that opens the Tideline capture overlay by spawning
the `tideline-capture.exe` helper, which writes `{"cmd":"capture"}` to
the resident app's named pipe (`\\.\pipe\tideline`).

Built against **Stream Deck SDK 6** (Node runtime), so the plugin can
actually shell out to the helper. The earlier SDK v2 (HTML / Chromium
sandbox) attempt could not spawn processes.

## Install

1. Download `Tideline.streamDeckPlugin` from a release.
2. Double-click the file. Stream Deck imports it and offers the
   **Capture to Tideline** action under the **Tideline** category.
3. Drag the action onto a button.
4. (Optional) Open the action's property inspector. The default
   `tideline-capture.exe` path resolves to the standard Velopack install
   location (`%LocalAppData%\Tideline\current\tideline-capture.exe`), so
   you only need to override it if you installed elsewhere.
5. Optionally fill in pre-filled text; the helper passes it to the app
   via `--text`.
6. Press the button. The capture overlay opens.

## Develop locally

```powershell
# From the plugin folder.
cd tools/streamdeck-plugin/com.tideline.capture.sdPlugin
npm install --omit=dev --no-audit --no-fund

# Build the helper and copy it next to the plugin entry.
dotnet publish ..\..\..\tools\Tideline.CaptureClient -c Release
Copy-Item ..\..\..\tools\Tideline.CaptureClient\bin\Release\net8.0\win-x64\publish\tideline-capture.exe bin\tideline-capture.exe -Force

# Hand the folder to Stream Deck (symlink or registerPlugin command).
```

## Layout

```
com.tideline.capture.sdPlugin/
├── manifest.json          SDK 6 manifest, declares Nodejs runtime
├── package.json           bundled ws dependency
├── package-lock.json
├── bin/
│   ├── plugin.js          plugin entry (Node)
│   └── tideline-capture.exe  bundled by the release workflow
├── ui/
│   └── inspector.html     property inspector
├── imgs/
│   ├── tidelineIcon.png
│   └── tidelineIcon@2x.png
└── node_modules/          installed at release pack time (not committed)
```

`node_modules/` is gitignored; the release workflow runs `npm ci --omit=dev`
inside the plugin folder before zipping the `.streamDeckPlugin` bundle.

## Why a custom plugin and not System -> Open

Stream Deck's built-in **System -> Open** action also works for a basic
"open overlay on press" button by pointing it at `tideline-capture.exe`.
The custom plugin adds:

- A discoverable Tideline category and action in the Stream Deck library.
- A per-button **Pre-filled text** option so different buttons capture
  different canned notes via `--text`.
- A default path that resolves to the standard Velopack install so most
  users do not have to type a path at all.
