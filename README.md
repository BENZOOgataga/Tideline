<div align="center">

# Tideline

**Time-aware notes that resurface with the tide.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6?logo=windows&logoColor=white)](#requirements)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/UI-WinUI%203-0078D6)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![Release](https://img.shields.io/github/v/release/BENZOOgataga/Tideline?include_prereleases&sort=semver)](https://github.com/BENZOOgataga/Tideline/releases)
[![Downloads](https://img.shields.io/github/downloads/BENZOOgataga/Tideline/total)](https://github.com/BENZOOgataga/Tideline/releases)
[![Issues](https://img.shields.io/github/issues/BENZOOgataga/Tideline)](https://github.com/BENZOOgataga/Tideline/issues)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![Local-first](https://img.shields.io/badge/local--first-yes-success)](#privacy)
[![No telemetry](https://img.shields.io/badge/telemetry-none-success)](#privacy)

</div>

---

Tideline is a desktop notes app in the spirit of Windows Sticky Notes, but **time-aware**. It is named for the mark water leaves on a shore: notes recede when you do not need them and resurface at the right moment, like the tide returning.

Every note remembers when it was written and can carry a future moment when it should come back. On launch, the app shows a curated briefing of what needs attention and frames resurfaced notes as a message from your past self, for example *"you wrote this on May 12, reminder to call the bank"*.

> The app replaces the common habit of sending messages to a private chat channel as a memo to your future self. It keeps that exact feeling, a stream of notes to yourself that you re-read later, but adds structure, automatic resurfacing of what matters, and a real sense of done.

## Screenshots

> _Screenshots coming with the first release._

| Briefing | Capture | The List |
|---|---|---|
| _placeholder_ | _placeholder_ | _placeholder_ |

---

## Why Tideline

- **Calm by design.** No constant polling, no notification spam, no wall of everything at once. Surfacing happens at a few deliberate moments only.
- **Progressive enrichment.** A note is not a fixed thing; it is a spectrum. A bare line is a quick thought. Add a deadline and it becomes a "do this on that day" item. Add references, files, and a checklist, and it becomes a working note. Same object the whole way up.
- **Capture is instant.** Sub-second capture from anywhere via a global hotkey or a Stream Deck button. It never asks you to decide anything.
- **Past-self framing.** Resurfaced notes are introduced in human terms, not as alerts.
- **Native, not a webpage in a frame.** Built on WinUI 3 with Mica backdrop and the system accent. Visually close to PowerToys.
- **Yours, forever.** Local-first SQLite. No accounts, no cloud, no telemetry.

## Features

- Global hotkey capture (`Ctrl+Alt+N` by default).
- Stream Deck plugin shipped as a `.streamDeckPlugin` bundle in releases.
- Ranked Briefing on launch, tray click, and window focus, with Overdue, Due today, Nudges, Aged someday, and Pinned buckets.
- The **List**, a single prioritised stream ordered by a date-driven score.
- The **Stream**, a chronological feed that reads like a chat log with your past self.
- **Spaces** for projects, **Tags** for cross-cutting themes. Two light layers, never nested folders.
- Inline natural-language dates (`tomorrow`, `next Friday`, `every Sunday`).
- Inline `#hashtags` parsed into tags as you type.
- Distinct **remind** time and **due** time. Two different concepts.
- Snooze with smart quick options (+1 hour, tonight, tomorrow, next week, custom). A gentle prompt appears after repeated snoozes so the app does not become a graveyard.
- Filter bar with **all** versus **any** tag matching, plus an inline filter language (`#work #urgent due:thisweek`).
- **Saved views** with optional mini-briefings.
- Attachments stored as references (files, folders, URLs); only pasted images are copied.
- Light markdown checklists.
- Light, dark, and system theme. Honors system font scaling.
- Native Windows toasts, summarised when several items need attention.
- Auto-start via Task Scheduler with a delayed log-on trigger (does not fight other startup programs for boot resources).
- Single-instance behavior: a second launch hands off to the resident process and exits.

## Privacy

Tideline is **local-first**. Data lives only in your SQLite file under `%LOCALAPPDATA%\Tideline\notes.db`. There is no telemetry, no analytics, no account, and no cloud sync. The only outbound network call is the opt-in update check.

Your data never leaves your machine.

## Install

### Requirements

- Windows 10 version 1809 or later, or Windows 11
- x64 architecture

### Download

Grab the latest installer from the [Releases page](https://github.com/BENZOOgataga/Tideline/releases).

### SmartScreen note

Tideline is currently distributed **unsigned** while the project is young. On first download or first run, Windows SmartScreen will show an *"unknown publisher"* warning. To proceed:

1. Click **More info**.
2. Click **Run anyway**.

This is expected. Code signing is tracked as a hardening step; see the SPEC for the plan.

### Stream Deck plugin

The `Tideline.streamDeckPlugin` bundle is published alongside each release. Double-click to install. It exposes a single action, *Capture to Tideline*, which opens the capture overlay. If Tideline is not running, the plugin launches it and retries once.

## Quick start

1. Install Tideline.
2. Press `Ctrl+Alt+N` from anywhere. A small input appears.
3. Type a thought. Optionally include a date phrase such as `tomorrow at 9am` or a hashtag like `#work`.
4. Press `Enter`. The note is saved.
5. Close the window. Tideline lives in the tray. Click the tray icon any time to re-open your briefing.

That is the whole loop. Everything else (Spaces, attachments, saved views, kanban lens, recurrence) is optional and added later, on your terms.

## How it differs from Sticky Notes, Todoist, Notion

- **Versus Sticky Notes.** Sticky Notes is a wall of cards with no priority and no resurfacing. Tideline collapses that wall into one ranked list and surfaces what matters when it matters.
- **Versus Todoist.** Todoist is a task manager that demands a decision at capture (project, label, priority). Tideline never asks. Notes earn structure only when you choose to add it.
- **Versus Notion.** Notion is a workspace. Tideline is a notes app. There are no nested folders, no databases, no blocks, no graph. One prioritised list, two light organising layers.

## Tech

| Layer | Choice |
|---|---|
| Language and runtime | C# on .NET 8 |
| UI | WinUI 3 (Windows App SDK) |
| Storage | SQLite via `Microsoft.Data.Sqlite` |
| Tray | `H.NotifyIcon.WinUI` |
| Hotkey | `RegisterHotKey` via P/Invoke |
| Local IPC | `System.IO.Pipes` named pipe |
| Notifications | `Microsoft.Windows.AppNotifications` |
| Auto-start | Windows Task Scheduler (delayed log-on trigger) |
| Date parsing | `Microsoft.Recognizers.Text.DateTime` |
| Packaging and self-update | Velopack |
| CI | GitHub Actions (on version tags) |

See [`docs/plans/SPEC.md`](docs/plans/SPEC.md) for the full design rationale.

## Building from source

See [CONTRIBUTING.md](CONTRIBUTING.md) for the full build steps, prerequisites, and contribution flow.

Short version:

```powershell
git clone https://github.com/BENZOOgataga/Tideline.git
cd Tideline
dotnet restore
dotnet build -c Release
```

## Roadmap

The build order from the SPEC, abbreviated:

1. Skeleton (WinUI 3 shell, SQLite, tray, single-instance)
2. Capture (hotkey, overlay)
3. The List and Stream
4. Time model (remind, due, recurrence, snooze)
5. Spaces and Tags
6. Briefing and scoring
7. Enrichment (checklists, attachments)
8. Filtering and saved views
9. Auto-start
10. IPC and Stream Deck plugin
11. Release pipeline and self-update
12. Polish

The full milestone list, including the optional kanban lens and the deferred calendar integration, lives in [`docs/plans/SPEC.md`](docs/plans/SPEC.md) sections 21 and 22.

## Contributing

Issues, ideas, and PRs are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) first, and note the [Code of Conduct](CODE_OF_CONDUCT.md).

For security issues, please follow [SECURITY.md](SECURITY.md) rather than opening a public issue.

## License

[MIT](LICENSE) (c) 2026-Present BENZOOgataga.
