# Handoff

Snapshot for any agent picking up the project. Read [`docs/plans/SPEC.md`](../plans/SPEC.md) for the canonical design, [`COMMON_ISSUES.md`](COMMON_ISSUES.md) for the traps already paid for, and [`ARCHITECTURE.md`](ARCHITECTURE.md) for the key code paths.

## Project state

- All SPEC milestones 1 through 13 are shipped. Milestone 14 (kanban lens) is deliberately not built; see SPEC section 22.
- 29 xunit Core tests pass. End-to-end IPC harness passes. UI smoke is manual.
- Public on GitHub at `BENZOOgataga/Tideline`, MIT, releases auto-built by GitHub Actions on `v*` tags.
- Latest release line: `v0.1.7`. Velopack auto-update is wired and verified to detect newer releases (the workflow now publishes the per-channel `releases.win.json` Velopack 0.0.1298 reads).

## What works

- Tray-resident, single-instance, close-to-tray, quit only via the tray menu.
- Capture from anywhere via `Ctrl+Alt+N` global hotkey or `tideline-capture.exe` over the local named pipe `\\.\pipe\tideline`.
- The List + Stream + Briefing, all backed by the SPEC section 8 date-driven score.
- Spaces, Tags, inline `#hashtag` parsing on capture, attachments as references, light markdown checklists with a progress chip.
- Inline filter language (`#work due:thisweek any:#idea,#someday space:learning`) and saved views.
- Auto-start via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` with `--startup` self-delay. Appears in Task Manager Startup apps; toggles in both directions stay in sync with Settings.
- System / Light / Dark theme, custom title bar with brand-aware logo and wordmark, debug-build dev brand (`Tideline Development` + orange wave logo, separate `%LocalAppData%\Tideline-dev` data dir).
- In-app update banner on launch when a newer release is downloaded by Velopack. Manual recheck via Settings and the tray menu.
- Release pipeline: tag push -> dotnet test + publish + vpk pack + GitHub Release with installer, full nupkg, RELEASES, per-channel manifests, and the Stream Deck plugin bundle.
- Stream Deck plugin built against SDK 6 (Node runtime), spawns the bundled helper, default exe path resolves to the standard Velopack install.

## What is deferred

Tracked in [`../../OPEN_QUESTIONS.md`](../../OPEN_QUESTIONS.md). Summary:

- SPEC section 22 product decisions (kanban grouping dimension, non-date ordering signals, distinct completed state).
- Inline natural-language date parsing in capture (currently uses date/time pickers in the Note edit dialog).
- Pasted-image attachments (only file/folder/URL references today).
- Summary toast notifications (SPEC section 15).
- Code signing (currently unsigned; SmartScreen "Run anyway" warning documented).

## How to build, run, test

Requires the **.NET 8 SDK** on Windows 10 1809+ or Windows 11. WinUI 3 ships bundled via `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`.

```powershell
dotnet restore Tideline.sln
dotnet test   tests/Tideline.Core.Tests/Tideline.Core.Tests.csproj
dotnet build  src/Tideline.App/Tideline.App.csproj -c Debug -r win-x64
dotnet build  tools/Tideline.CaptureClient/Tideline.CaptureClient.csproj -c Debug
powershell -ExecutionPolicy Bypass -File tools/harness/Run-IpcSmoke.ps1
```

All four should be green. The harness exercises capture end-to-end through the named pipe in an isolated `TIDELINE_DATA_DIR`.

Run the local debug build:

```powershell
.\src\Tideline.App\bin\Debug\net8.0-windows10.0.19041.0\win-x64\Tideline.exe
```

Debug builds resolve data to `%LocalAppData%\Tideline-dev\`. Installed release builds use `%LocalAppData%\Tideline\`.

## How to cut a release

See [`RELEASING.md`](RELEASING.md). Short version: a maintainer pushes a `v*` tag, GitHub Actions does the rest. **Agents do not initiate releases** (see [`../../CLAUDE.md`](../../CLAUDE.md) "Releases" section).

## Recovery notes from earlier sessions

- The local `feat/v1-core` branch was once created by renaming local `main`, so it kept tracking `origin/main`. Combined with a git-extension that auto-pushed on commit, this once silently pushed the first scaffold commit to `main`. The branch was retracked and from then on every feature commit went only to `feat/v1-core`. Make sure local branch tracking is correct before pushing.
- The very first releases (v0.1.0 through v0.1.5) shipped a broken Velopack locator because the workflow installed the latest `vpk` instead of the version pinned in the app. Installs from before v0.1.6 cannot auto-update and must be reinstalled.
- The Stream Deck plugin's `bin/plugin.js` was once missing from the bundle because it sat under `bin/` which matches the .NET build-output gitignore. The `.gitignore` now explicitly lifts that subtree.

## When you stop work

- Leave `main` in a state where `dotnet test` and the harness pass.
- Update [`COMMON_ISSUES.md`](COMMON_ISSUES.md) with any new trap you paid for.
- Update [`ARCHITECTURE.md`](ARCHITECTURE.md) if you changed a core flow.
- Update this file's "What works" / "What is deferred" lines if you moved the line.
- Do NOT cut a release on your own initiative.
