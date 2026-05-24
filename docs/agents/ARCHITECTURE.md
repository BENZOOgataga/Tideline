# Architecture

How the running app is shaped and the flows that cross module boundaries. Pair with [`../plans/SPEC.md`](../plans/SPEC.md) for the why.

## Layers

```
+---------------------------------------------------------+
| Tideline.App  (WinUI 3, net8.0-windows10.0.19041.0)     |
|  - App.xaml.cs       composition root + lifecycle        |
|  - Program.cs        custom Main, single-instance        |
|  - Views/            pages, dialogs, capture overlay     |
|  - ViewModels/       NoteCard etc.                       |
|  - Services/                                             |
|     AppHost          owns DB + repositories              |
|     HotkeyService    Ctrl+Alt+N via P/Invoke             |
|     CaptureService   overlay lifetime                    |
|     IpcListener      named pipe \\.\pipe\tideline        |
|     TrayHost         H.NotifyIcon                        |
|     AutoStartService HKCU\Run + StartupApproved          |
|     UpdateService    Velopack GithubSource               |
|     ThemePreference  System / Light / Dark               |
|     Brand            #if DEBUG asset + name resolver     |
|     CrashLog         best-effort log under AppPaths.Root |
|  - Interop/Win32.cs  P/Invoke surface                    |
+---------------------------------------------------------+
                            |
                            v
+---------------------------------------------------------+
| Tideline.Core  (pure net8.0 class library)              |
|  - Models/           Note, Space, Tag, Attachment        |
|  - Data/             SQLite schema, repositories, purge  |
|  - Filtering/        FilterQuery, FilterParser           |
|  - Parsing/          HashtagParser, ChecklistParser      |
|  - Time/             IClock, RelativeTime, SnoozeOptions |
|  - Briefing/         BriefingService                     |
+---------------------------------------------------------+
                            |
                            v
                  SQLite (Microsoft.Data.Sqlite)
                  at AppPaths.DatabasePath
```

Tests live in `tests/Tideline.Core.Tests/` and only target the Core library (no UI), against a real SQLite file per test.

Tools live in `tools/`:
- `Tideline.CaptureClient/` -> the `tideline-capture.exe` helper, shipped alongside `Tideline.exe`.
- `harness/Run-IpcSmoke.ps1` -> end-to-end IPC test.
- `streamdeck-plugin/` -> Stream Deck SDK 6 plugin scaffold.

## Data paths

`Tideline.Core.Data.AppPaths`:
- `TIDELINE_DATA_DIR` env var overrides everything (used by the harness).
- Debug builds: `%LocalAppData%\Tideline-dev`.
- Release builds: `%LocalAppData%\Tideline`.

Layout inside the data dir:
```
notes.db           SQLite (WAL mode)
notes.db-shm
notes.db-wal
attachments/       reserved for image_copied attachments
theme.txt          theme preference
autostart-migrated marker for the schtasks -> Run migration
crash.log          best-effort exception log
```

Velopack also lives at `%LocalAppData%\Tideline\` on Release installs:
```
Update.exe
current\Tideline.exe         <- the installed exe
current\tideline-capture.exe <- bundled helper
packages\*.nupkg
```

So the **release** install dir contains both Velopack files and Tideline data side by side. This is intentional and works fine because the names do not collide.

## Lifecycle

```
launch
  -> Program.Main
       VelopackApp.Build().Run()   handles --veloapp-* hooks, may early-exit
       parse --startup arg         -> Thread.Sleep(8s) if set
       ComWrappersSupport.Initialize()
       AppInstance.FindOrRegisterForKey  single-instance check
         -> redirected? exit
       Application.Start
         -> new App()
  -> App.OnLaunched
       AppHost.Boot                 opens SQLite, runs ArchivePurge
       AutoStartService.MigrateLegacyTaskIfPresent
       CaptureService, HotkeyService, MainWindow, TrayHost
       IpcListener.Start
       UpdateService (and CheckOnceFireAndForget)
       ShowMainWindow             unless --startup
```

Single instance: second launches redirect activation to the first instance via `Microsoft.Windows.AppLifecycle.AppInstance.RedirectActivationToAsync`, then exit. The first instance handles `Activated` by surfacing the main window.

Quit only happens via the tray menu. The main window's `AppWindow.Closing` event cancels close and hides to tray, unless `App.IsShuttingDown` is set by `QuitApp`.

## Capture flow

1. **Trigger.** `Ctrl+Alt+N` (HotkeyService), tray menu "Capture note", or `tideline-capture.exe` writing `{"cmd":"capture"}` to `\\.\pipe\tideline`.
2. **CaptureService.Show()** raises the `CaptureWindow` (one at a time; re-triggers re-focus it).
3. **CaptureWindow** uses `OverlappedPresenter.CreateForContextMenu` (borderless), `DesktopAcrylicBackdrop`, `IsAlwaysOnTop = true`. On `Activated` it force-foregrounds via `AttachThreadInput` + `SetForegroundWindow` and focuses the TextBox.
4. **Enter** -> `NoteRepository.Create(body)` (writes `notes` row, mirrors to the FTS5 virtual table). Inline `#hashtags` parsed via `HashtagParser` and attached via `TagRepository.ReplaceForNote`.
5. **Deactivation closes the overlay** so it never lingers.

## Briefing flow

Single computation on demand (no background loop). Triggered on launch, tray click, window focus.

`BriefingService.Compute()` walks `NoteRepository.All()` and bucketises:
- Pinned: `pinned = 1`
- Overdue: `due_at < startOfTodayMs`, score = `BaseOverdue + daysOverdue * weight`
- DueToday: `due_at` within today
- Nudges: `remind_at <= now` (including dated nudges)
- AgedSomeday: `space_id IS NULL` AND `due_at IS NULL` AND `created_at < now - SOMEDAY_AGE_DAYS`

Ordering: bucket rank first (Pinned, Overdue, DueToday, Nudges, AgedSomeday), then descending score. **No snooze-as-signal or undated-aging signals** (deliberately, per SPEC section 22 open question).

## IPC + capture helper

`IpcListener` hosts `\\.\pipe\tideline` with `PipeOptions.CurrentUserOnly`. Accepts line-delimited JSON commands:
- `{"cmd":"capture"}` -> opens the overlay
- `{"cmd":"capture","text":"..."}` -> writes the note inline without UI
- `{"cmd":"show"}` -> raises the main window
- `{"cmd":"count","includeArchived":false}` -> returns `{"count":N}`

All commands are marshalled to the UI dispatcher via `MarshalToUi` because `SqliteConnection` is not thread-safe.

`tideline-capture.exe` is a self-contained CLI in `tools/Tideline.CaptureClient/`. The release workflow copies the published exe both into the Velopack install (`current\tideline-capture.exe`) and into the Stream Deck plugin bundle (`bin\tideline-capture.exe`).

## Auto-start flow

`AutoStartService`:
- `IsEnabled` reads `HKCU\...\Run\<Brand.DisplayName>` AND checks the `StartupApproved\Run\<Name>` blob's low bit.
- `Enable(exePath)` writes the Run value (`"<exe>" --startup`), sets StartupApproved to enabled, sweeps the legacy `Tideline-AutoStart` schtasks entry.
- `Disable()` removes both Run value and StartupApproved entry, sweeps the legacy task.
- `MigrateLegacyTaskIfPresent` runs once at boot, gated by a marker file under `AppPaths.Root`. Detects the legacy task and converts to Run if needed.

`Program.Main` reads the `--startup` flag and self-delays 8 seconds so the app does not fight other startup programs for boot resources.

## Update flow

`UpdateService` wraps Velopack `UpdateManager(new GithubSource("https://github.com/BENZOOgataga/Tideline"))`.

On launch (`CheckOnceFireAndForget`, once per session) and on user request (`RecheckFireAndForget`, bypasses the guard):
1. `CheckForUpdatesAsync` -> `UpdateInfo?`
2. If non-null, `DownloadUpdatesAsync` in the background
3. Fire `UpdateReady` on the UI dispatcher
4. Always fire `CheckCompleted` in `finally`

`MainWindow` subscribes to `UpdateReady` and surfaces the InfoBar banner; `SettingsPage` subscribes to `CheckCompleted` and re-runs `PopulateAbout`.

The banner's "Restart and install" button calls `ApplyAndRestart` which is `UpdateManager.ApplyUpdatesAndRestart(UpdateInfo)`.

## Release flow

GitHub Actions on `v*` tag push:
1. `actions/checkout`, `setup-dotnet 8`, `setup-node 20`
2. Derive version from `${{ github.ref_name }}`
3. `dotnet restore`, `dotnet test`, `dotnet publish` app + capture client self-contained for `win-x64`
4. Copy `tideline-capture.exe` into the app publish dir so the Velopack install carries the helper
5. `dotnet tool install -g vpk --version 0.0.1298` (must match the in-app Velopack NuGet)
6. `vpk pack --packId Tideline --packVersion <version> --packDir publish/app --mainExe Tideline.exe`
7. Bundle the Stream Deck plugin: `npm ci --omit=dev` inside the plugin folder, copy the helper exe in, zip as `.streamDeckPlugin`
8. `softprops/action-gh-release@v2` uploads `releases/*.nupkg`, `releases/*-Setup.exe`, `releases/RELEASES`, `releases/releases.*.json`, `releases/assets.win.json`, `Tideline.streamDeckPlugin`

The per-channel `releases.win.json` and `assets.win.json` files are what Velopack 0.0.1298's `GithubSource` actually reads. Without them, the in-app check finds no updates.

## Hard contracts (do not regress)

- `notes.created_at` is immutable. Set once on `Create`, never touched again. The temporal framing depends on it.
- All timestamps are Unix milliseconds in UTC. Render local.
- `SqliteConnection` is owned by the UI thread of the resident app. IPC handlers MUST marshal to the UI dispatcher before touching repositories.
- `ArchivePurge` runs at launch only. Deletes only archived non-pinned notes whose `archived_at` is older than the retention threshold.
- Briefing score is date-driven only. Do not add snooze-as-signal or undated-aging signals until SPEC section 22 is resolved.
- Optional kanban lens is NOT built. SPEC section 22 grouping question is open.
- Agents do not initiate releases. Tag pushes are public and require explicit user request (CLAUDE.md "Releases").
