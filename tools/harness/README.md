# Tideline harness

Hand-rolled smoke and integration scripts that exercise the built app
without driving its UI. They are not a substitute for the xunit Core
suite under [`../tests/`](../../tests/); they cover what the unit tests
cannot, namely the resident app, the named-pipe IPC channel, and the
single-instance behavior.

## Scripts

### `Run-IpcSmoke.ps1`

Launches the built `Tideline.exe` against a fresh `TIDELINE_DATA_DIR`,
waits for the named pipe `\\.\pipe\tideline` to appear, sends
`{"cmd":"capture","text":"..."}` through the `tideline-capture` helper,
queries the resulting `notes.db` for the marker text, and tears the app
down. Exits non-zero on any failure.

Run:

```powershell
pwsh tools/harness/Run-IpcSmoke.ps1
```

Build prerequisites:

```powershell
dotnet build src/Tideline.App/Tideline.App.csproj -c Debug -r win-x64
dotnet build tools/Tideline.CaptureClient/Tideline.CaptureClient.csproj -c Debug
```

The script uses the `TIDELINE_DATA_DIR` environment variable, which
`Tideline.Core.Data.AppPaths` honors as an override of the default
`%LOCALAPPDATA%\Tideline` location. That keeps the harness fully isolated
from any real user data.
