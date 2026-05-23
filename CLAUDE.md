# Agent Instructions

## Additional Instruction Sources

- Treat this file as the root agent entry point.
- Also check the root-level `.claude/` and `.agents/` directories for additional agent instructions and apply any relevant rules you find there.
- Re-check those directories when working in areas that may have more specific local guidance.

## Character Style

Match the prose character rule defined in [docs/plans/SPEC.md](docs/plans/SPEC.md):

- Use only straight quotes and basic punctuation in prose.
- Do not use em dashes, en dashes, the ellipsis character, or smart quotes.
- Code, identifiers, and config syntax are exempt.

## Commits

- Use Conventional Commits only.
- Keep commit subjects lowercase, in the imperative mood, with no trailing period.
- Do not add co-authors or `Co-authored-by` trailers.

## Source of truth

[`docs/plans/SPEC.md`](docs/plans/SPEC.md) is the canonical design.
[`OPEN_QUESTIONS.md`](OPEN_QUESTIONS.md) lists everything v1 deliberately
did not decide. Do not silently guess on a fork in the spec; add to
OPEN_QUESTIONS and pick the most minimal, reversible option.

## Solution layout

```
Tideline.sln
global.json                                  pin .NET SDK
Directory.Build.props                        shared LangVersion, Version
src/
  Tideline.Core/                             pure C# net8.0 library
    Briefing/BriefingService.cs              date-driven score, buckets
    Data/                                    sqlite, repositories, schema, purge
    Filtering/                               FilterQuery, FilterParser
    Models/                                  Note, Space, Tag, Attachment, SavedFilter
    Parsing/                                 HashtagParser, ChecklistParser
    Time/                                    IClock, RelativeTime, SnoozeOptions
  Tideline.App/                              WinUI 3 head, net8.0-windows10.0.19041.0
    App.xaml, App.xaml.cs                    bootstrap, owns Host and services
    Program.cs                               custom Main, AppInstance single-instance
    Interop/Win32.cs                         P/Invoke for global hotkey
    Services/                                AppHost, Capture, Hotkey, Tray, IPC, AutoStart, Theme, Update
    Views/                                   MainWindow + pages + dialogs
    Assets/Tideline.ico                      placeholder icon
    app.manifest                             dpiAware, supportedOS
tests/
  Tideline.Core.Tests/                       xunit, hits a real sqlite per test
tools/
  Tideline.CaptureClient/                    small helper that talks to the named pipe
  harness/                                   PowerShell smoke (Run-IpcSmoke.ps1)
  streamdeck-plugin/                         Stream Deck plugin scaffold
.github/workflows/release.yml                tag-driven release with vpk pack
```

## Build, run, test

Requires .NET 8 SDK on Windows 10 1809 or later. WinUI 3 is bundled via
`<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>` so no
separate Windows App SDK runtime install is required for development.

```powershell
dotnet restore Tideline.sln
dotnet build src/Tideline.App/Tideline.App.csproj -c Debug -r win-x64
dotnet test  tests/Tideline.Core.Tests/Tideline.Core.Tests.csproj
```

Run from CLI:

```powershell
src\Tideline.App\bin\Debug\net8.0-windows10.0.19041.0\win-x64\Tideline.exe
```

Run with an isolated data directory (e.g. for testing):

```powershell
$env:TIDELINE_DATA_DIR = "C:\Temp\tideline-scratch"
src\Tideline.App\bin\Debug\net8.0-windows10.0.19041.0\win-x64\Tideline.exe
```

`AppPaths.cs` honors `TIDELINE_DATA_DIR`; default is
`%LOCALAPPDATA%\Tideline`.

End-to-end harness (launches the app, sends a capture via the named pipe,
verifies the note count, cleans up):

```powershell
dotnet build tools/Tideline.CaptureClient/Tideline.CaptureClient.csproj -c Debug
powershell -ExecutionPolicy Bypass -File tools/harness/Run-IpcSmoke.ps1
```

## Critical contracts (do not regress)

- `created_at` is immutable. Set once on Create, never touched again.
- All times are Unix milliseconds in UTC.
- SQLite is single-writer; the resident `Tideline.App` process owns the
  connection. IPC handlers marshal to the UI dispatcher before touching
  `Host.Notes` or any repository ([`IpcListener.MarshalToUi`](src/Tideline.App/Services/IpcListener.cs)).
- Archive purge runs at launch only, deletes only archived notes past
  the threshold, and never deletes pinned notes
  ([`Tideline.Core.Data.ArchivePurge`](src/Tideline.Core/Data/ArchivePurge.cs)).
- Briefing scoring is date-driven only. Do not add snooze-as-signal or
  age-undated-notes signals until SPEC section 22 is resolved.
- Optional kanban lens is not built (SPEC milestone 14, skipped on
  purpose).
