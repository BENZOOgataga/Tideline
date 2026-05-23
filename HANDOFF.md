# Handoff

Tideline v1 core, built in one autonomous session on `feat/v1-core`.
The branch compiles and runs end-to-end. `main` is untouched after the
initial recovery described below.

## Branch state

- Local and `origin/feat/v1-core` are in sync.
- `origin/main` is left at commit `2577c8c`, which is the first scaffold
  commit. That commit landed on main accidentally because the local
  `feat/v1-core` branch was created by renaming local `main` and kept
  tracking `origin/main`, and VS Code's git integration auto-pushed it.
  Tracking has been corrected; every later commit went only to
  `feat/v1-core`. Owner decided to leave main at `2577c8c` rather than
  force-push back to the pre-build state.
- Disable VS Code's `git.postCommitCommand` (or whichever extension is
  auto-pushing) before merging to avoid the same trap on future work.

## Milestones (SPEC section 21)

| # | Milestone | State |
|---|-----------|-------|
| 1 | Skeleton: WinUI 3 shell, Mica, NavigationView, SQLite schema, tray, close-to-tray, single instance | done |
| 2 | Capture: global hotkey, overlay, save bare note | done |
| 3 | The List and Stream: prioritised List, chronological Stream, edit, archive, FTS5 search | done |
| 4 | Time model: remind_at, due_at, recurrence (RRULE string), reschedule, snooze quick options | done |
| 5 | Organization: Spaces, Tags, Inbox default, inline `#hashtag` parser, Space colors | done |
| 6 | Briefing and scoring: date-driven score, Pinned, Overdue, Due today, Nudges, Aged someday buckets, empty state | done |
| 7 | Enrichment: checklist parser and progress chip, attachments as references (file, folder, URL) | done; image paste capture not yet implemented (OPEN_QUESTIONS) |
| 8 | Filtering and saved views: filter bar, inline filter language, all-vs-any tag mode, saved views CRUD | done; saved-view mini-briefings (`feeds_briefing`) is wired in the schema but not surfaced in the UI yet |
| 9 | Auto-start: Task Scheduler logon trigger with 8 second delay, Settings toggle | done |
| 10 | IPC and Stream Deck: named pipe `\\.\pipe\tideline`, Stream Deck plugin bundle | listener done; the `.streamDeckPlugin` is a scaffold (no icon set; see OPEN_QUESTIONS) |
| 11 | Release pipeline: Velopack via GitHub Actions on version tags, self-update on launch | workflow done; in-process self-update is a placeholder (OPEN_QUESTIONS) |
| 12 | Open source scaffolding: LICENSE, README, CONTRIBUTING, SECURITY | already present pre-build; verified |
| 13 | Polish: Fluent motion, theming, accent integration, summary toasts | partial: System/Light/Dark theme toggle done. Summary toasts not yet emitted (OPEN_QUESTIONS) |
| 14 | Optional kanban lens | deliberately NOT built per brief; the SPEC section 22 grouping question is unresolved |

## How to build

Requires .NET 8 SDK on Windows 10 1809+ (or Windows 11) x64. No Visual
Studio install needed. The Windows App SDK runtime is bundled via
`WindowsAppSDKSelfContained=true`.

```powershell
dotnet restore Tideline.sln
dotnet build  src/Tideline.App/Tideline.App.csproj -c Debug -r win-x64
dotnet build  tools/Tideline.CaptureClient/Tideline.CaptureClient.csproj -c Debug
dotnet test   tests/Tideline.Core.Tests/Tideline.Core.Tests.csproj
```

The session built with .NET SDK `10.0.300` (forward-compatible with
`net8.0` targets) because `winget install Microsoft.DotNet.SDK.8` hit
`1618: Another installation is in progress` during the session. Install
the .NET 8 SDK once the MSI lock clears, and `global.json` will prefer
it (it pins `8.0.0` with `rollForward: latestMajor`, `allowPrerelease`).

## How to run

```powershell
src\Tideline.App\bin\Debug\net8.0-windows10.0.19041.0\win-x64\Tideline.exe
```

The app stays resident in the tray; closing the window does not exit
(SPEC section 10). Quit via the tray menu.

To exercise with isolated data:

```powershell
$env:TIDELINE_DATA_DIR = "C:\Temp\tideline-scratch"
.\src\Tideline.App\bin\Debug\net8.0-windows10.0.19041.0\win-x64\Tideline.exe
```

`AppPaths.Root` honors `TIDELINE_DATA_DIR`. Default is
`%LOCALAPPDATA%\Tideline`.

## How to test

- Unit and integration suite: `dotnet test tests/Tideline.Core.Tests/`.
  29 tests covering schema, repositories, archive purge, snooze, tags,
  spaces, filter parser and query, briefing scoring, checklist parser,
  and relative-time framing.

- End-to-end harness: `tools/harness/Run-IpcSmoke.ps1`. Boots the built
  app in a fresh `TIDELINE_DATA_DIR`, waits for the named pipe to come
  up, sends `{"cmd":"capture","text":"..."}` through the
  `tideline-capture` helper, asks for the resulting note count back over
  the same pipe, and tears the app down. Exits non-zero on any failure.
  Run:

  ```powershell
  powershell -ExecutionPolicy Bypass -File tools/harness/Run-IpcSmoke.ps1
  ```

## What the owner must finish

See [`OPEN_QUESTIONS.md`](OPEN_QUESTIONS.md) for the full list. Summary:

- **SPEC section 22 open product decisions** (kanban grouping, non-date
  ordering signals, completion model). The agent did not decide any of
  these. The optional kanban lens is therefore not built.
- **Velopack version pin** for in-process self-update. Workflow already
  uses `vpk` as a global tool to pack on tag push, so a tagged release
  will produce installer artifacts, but the running app does not yet
  call `UpdateManager`.
- **Stream Deck plugin icon set.** Manifest references `tidelineIcon`;
  no PNG asset is committed. Stream Deck falls back to a default icon.
- **App icon polish.** `src/Tideline.App/Assets/Tideline.ico` is a
  procedural flat-color square. Replace with a real icon set.
- **Inline natural-language date parsing.** SPEC section 12 wants
  `Microsoft.Recognizers.Text.DateTime`; v1 ships explicit
  `DatePicker`/`TimePicker` controls in the note edit dialog instead.
- **Summary toasts** (SPEC section 15) are not yet emitted.
- **Code signing** (SPEC section 19.3). v1 ships unsigned; README
  already documents the SmartScreen "Run anyway" path.
- **Saved view mini-briefings** (`saved_filters.feeds_briefing`). The
  column exists; the UI does not yet expose the per-view briefing.

## Recovery action items already done

- Local `feat/v1-core` tracking re-pointed at `origin/feat/v1-core`.
- All milestone commits pushed only to `feat/v1-core`.
- `OPEN_QUESTIONS.md` created at the repo root and updated each time the
  build hit a fork the SPEC did not answer.
- `CLAUDE.md` updated with the new project layout and how to build, run,
  and test. The original character-style and commit conventions are
  preserved.

## One quick sanity run before merging

```powershell
dotnet test  tests/Tideline.Core.Tests/Tideline.Core.Tests.csproj
dotnet build src/Tideline.App/Tideline.App.csproj -c Debug -r win-x64
dotnet build tools/Tideline.CaptureClient/Tideline.CaptureClient.csproj -c Debug
powershell -ExecutionPolicy Bypass -File tools/harness/Run-IpcSmoke.ps1
```

All four should be green. If the harness ever shows "Pipe is broken",
something regressed in `Services/IpcListener.cs` (see commit
`3056f13`'s fix for the pipe lifetime bug).
