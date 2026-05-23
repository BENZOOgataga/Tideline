# Tideline. Specification and Design Plan

*Time-aware notes that resurface with the tide.*

**Version:** 2.0
**Target builder:** Autonomous coding agent
**Platform:** Windows 10 version 1809 and later, and Windows 11, x64. Local-first, single user per install.
**Project type:** Open source. Built primarily for the author, but published publicly under a permissive license for anyone to use, fork, or contribute to. See section 18.

> Document conventions: this spec follows a strict character style. It uses only straight quotes and basic punctuation. It contains no em dash, en dash, ellipsis character, or smart quotes. Any code, identifier, or config syntax is exempt from that style.

---

## 1. Concept

A desktop notes app in the spirit of Windows Sticky Notes, but time-aware. Tideline is named for the mark water leaves on a shore: notes recede when you do not need them and resurface at the right moment, like the tide returning. Every note remembers when it was written and can carry a future moment when it should come back. On launch the app shows a curated briefing of what needs attention, with plain factual framing such as "written on May 12, reminder for today". The feeling is a quiet note from earlier-you, but the wording stays factual and never speculates, see section 1.3.

The deeper idea is **progressive enrichment**. A note is not a fixed thing; it is a spectrum. A bare line is a quick thought. Add a deadline and it becomes a "do this on that day" item. Add references, files, and a checklist, and it becomes a richer working note. It is the same object the whole way up, just carrying more context, and it always stays a single entry in one prioritised list rather than becoming a card on a board. This is the spine of the app, and it sits on one rule that must never be broken: capture is instant and never asks you to decide anything, while enrichment is always optional and always added later. That rule is what keeps Tideline calm instead of turning into a heavyweight tool.

The app replaces the common habit of sending messages to a private chat channel as a memo to your future self. It keeps that exact feeling, a stream of notes to yourself that you re-read later, but adds structure, automatic resurfacing of what matters, and a real sense of done.

The app is calm by design: no constant polling, no notification spam, no wall of everything at once. It surfaces things at a few deliberate moments only. See section 10.

### 1.1 Identity, what Tideline is and is not

The shortest accurate description is **a personal operational memory**: a resurfacing note system for long-term personal work continuity. Put another way, it is the private message-to-yourself habit, done properly for the long term. The app exists because useful things get buried in a private chat after they are sent: reminders, resources, references to credentials, files, infrastructure notes, debugging follow-ups, links, and temporary operational thoughts. Tideline solves that while preserving the same emotional simplicity and low friction.

Tideline IS: a personal operational memory, a lightweight temporal note system, a resurfacing note application, a low-friction capture environment, a documentation and reminders hybrid, and a private note stream to yourself.

Tideline is NOT: a task manager, a project management tool, a workspace platform, a productivity operating system, an AI assistant, a smart second brain, a Notion alternative, or a Todoist clone.

### 1.2 Philosophy, the feel that must be protected

The app must never feel like enterprise software, database software, admin tooling, life-management software, or productivity coaching. It must feel calm, lightweight, personal, quiet, non-judgmental, low-pressure, and immediate. The user must never feel "I need to maintain the system." If they do, that is a product failure, not a missing feature. Advanced organization emerges progressively and stays hidden until wanted; the interface prioritizes note content and temporal framing over metadata, categorization, and controls. The visual feeling to aim for is a desktop surface holding resurfacing memory fragments, not productivity software.

### 1.3 No faked intelligence

Tideline is intentionally not AI-driven. It does not infer emotional context, analyze behavior, generate insights, interpret meaning, predict patterns, or act like an assistant. The resurfacing system is simple and deterministic, based on remind dates, due dates, lateness, and optional future aging or snooze signals. The app must never fake intelligence it does not have. This has a direct consequence for copy, see section 23: prefer plain factual framing such as "written 11 days ago" or "reminder for today", and never claim to know what the user was doing, for example "you wrote this while working on infrastructure". The earlier shorthand "a message from your past self" is a design feeling, not literal copy; the shipped wording stays factual.

### 1.4 Emotional and visual references

Lean toward the feel of Windows Sticky Notes, Telegram saved messages, a chat with yourself, Apple Journal, PowerToys, the restraint of Things 3, and the calm of Arc. Avoid drifting toward Notion, ClickUp, Jira, Monday, Airtable, a Linear clone, database dashboards, an Obsidian clone, AI second-brain software, or PKM maximalism. Sticky Notes is the strongest emotional reference for low pressure, instant capture, visual simplicity, and disposability; Tideline keeps those qualities while adding resurfacing, light organization, references, filtering, and operational continuity, without becoming visually heavy.

---

## 2. Goals and Non-Goals

### Goals
- Sub-second capture of a thought from anywhere via a global hotkey or a Stream Deck button.
- Progressive enrichment: any note can grow from a bare line into a richer working note without changing type or leaving the single list.
- Resurface notes at the right time with human, contextual framing, ranked so the most important float up.
- Separate the nudge time from the deadline. These are different concepts.
- Organize without clutter: light two-layer structure of Spaces and Tags, never nested folders.
- Strong filtering and saved views so nothing is lost once the pile grows large.
- Launch at log on cleanly, without competing for boot resources.
- Notes persist and stay searchable; completed notes are archived, hidden but resurfaceable, and archived notes are purged after 90 days. See section 6.
- Local-first and private: all data stays on the machine. No telemetry, no analytics, no accounts. The only outbound network call is the opt-in update check.
- Feel like a real, native Windows program, visually close to PowerToys, never like a website in a frame.

### Non-Goals for version 1
- No cloud sync, accounts, or multi device.
- No collaboration or sharing.
- No mobile app in version 1. A future mobile companion is possible but strictly secondary. See section 24.
- No nested folders or subfolders.
- No rich text editor, custom fonts per note, or media embeds beyond plain text with light markdown.
- No links between notes or graph view. That is wiki territory and belongs in a different app.
- No calendar grid view. The briefing is the deliberate opposite of a calendar.
- No calendar sync in version 1. See section 20 for the deferred plan and the platform caveat.
- No artificial intelligence features in version 1.

---

## 3. Tech Stack

| Layer | Choice | Rationale |
|---|---|---|
| Language and runtime | C# on .NET 8 | Best-in-class Windows integration, mature tooling |
| UI framework | WinUI 3 via the Windows App SDK | Native Fluent controls, Mica backdrop, system accent, the exact PowerToys look, for free |
| Storage | SQLite via `Microsoft.Data.Sqlite` | Local, file-based, queryable, zero config |
| Tray icon | `H.NotifyIcon.WinUI` | WinUI 3 has no built-in tray; this is the standard community library |
| Global hotkey | `RegisterHotKey` via P/Invoke | Reliable system-wide hotkey |
| Local IPC | `System.IO.Pipes` named pipe | Built into .NET, no open port, OS-enforced access. See section 16 |
| Notifications | `Microsoft.Windows.AppNotifications` from the Windows App SDK | Native toasts |
| Auto-start | Windows Task Scheduler via `schtasks` or the TaskScheduler library | Supports a delayed log-on trigger, which the Run registry key does not |
| Date parsing | `Microsoft.Recognizers.Text.DateTime` | Natural-language date extraction |
| Packaging, install, self-update | Velopack | One library that builds the installer and handles self-update from GitHub releases. See section 19 |
| CI | GitHub Actions | Triggered on a version tag |

### Why WinUI 3, and the tradeoff accepted
The reference target is the PowerToys settings window, which is built on WinUI 3. The Mica backdrop, the Fluent toggles and chevrons, the sidebar styling, and the system accent integration are all built into WinUI and come essentially for free. Reproducing that look in a cross-platform toolkit would mean hand-building approximations that never quite match the real system materials.

The accepted cost: a WinUI 3 app idles heavier than a native lightweight toolkit, on the order of tens of megabytes more, and starts slightly slower. This was a deliberate reversal of an earlier lean-runtime preference, made because feeling like a genuine Windows app is the deeper requirement, and a tray-resident notes app at that footprint is not a real problem on any machine that already runs PowerToys.

---

## 4. Design Language

The target is a calm, monochrome, high-whitespace interface that is unmistakably a native Windows app. The reference is PowerToys. The aesthetic influence is Geist, the design language from Vercel, taken as restraint and discipline rather than as a framework, since Geist itself is web only and is not used directly.

### From Windows, the nativeness
- Use the **Mica** backdrop on the main window via `SystemBackdrop`, exactly as PowerToys does.
- Respect the user's light and dark mode from Windows settings, and follow the system **accent color** for active and selected states.
- Use native Fluent controls: toggle switches, navigation view sidebar, chevrons, content cards floating on the backdrop, hairline separators.
- Follow Fluent motion and easing so transitions feel like Windows, not like web CSS.
- Use native window chrome, the proper tray surface, and native toast notifications.
- Honor system font scaling for accessibility.

### From Geist, the restraint
- Monochrome first, with the single system accent as the only color emphasis.
- High contrast and generous whitespace.
- Thin hairline borders, small and deliberate corner radii, subtle shadows rather than dramatic ones.
- A clean type pairing: a crisp sans for body text, a monospace for timestamps and metadata. Ship the Geist Sans and Geist Mono fonts, which are free to use, to carry the feeling without importing any web framework.

### Layout shape, modeled on PowerToys
- A left navigation sidebar with a subtle selection highlight.
- Top-level destinations in the sidebar: Briefing, the List, Stream, then the list of Spaces, then Tags, then Saved Views, then Settings.
- Content shown as rounded cards on the Mica backdrop, with generous padding.
- A search and filter bar pinned at the top of content views.

The synthesis in one line: Geist restraint expressed through native Fluent and Mica, so the app looks custom and considered, clearly not stock Windows, yet clearly a real program rather than a webpage.

---

## 5. Core Model: Progressive Enrichment

A single object, the **note**, grows along an enrichment ladder. There is no separate task type. A note becomes task-like the moment it carries a due date and reverts to a plain thought when it does not. A richer note is still just a richer entry in the same prioritised list. It does not become a card on a board.

The ladder, from line to richer note:
1. **Text only.** A thought. Unfiled, weightless, lives in the Inbox.
2. **Plus a Space or a tag.** Now it is filed.
3. **Plus a remind time or a due time.** Now it is a "do this on that day" item, and can resurface and rank in the list.
4. **Plus a checklist.** Light markdown checkboxes give it internal steps.
5. **Plus references.** Images, files, links, and folder paths on the PC make it a working surface for real work. See section 13.

The guardrail: enrichment is never demanded at capture. Capture saves a bare line in one keystroke. Everything above level 1 is added later, on the user's terms. No matter how enriched a note becomes, it stays a line in the one prioritised list. There is no kanban and no workflow status in the core. Kanban exists only as an optional view lens described in section 9.

---

## 6. Data Model

A single SQLite database at `%LOCALAPPDATA%\Tideline\notes.db`. All times are stored as Unix milliseconds in UTC and rendered in local time.

### Table: `notes`
| Column | Type | Notes |
|---|---|---|
| `id` | TEXT, UUID | Primary key |
| `body` | TEXT | Note content, plain text with light markdown |
| `created_at` | INTEGER | Set once at creation, immutable. The temporal framing depends on it |
| `updated_at` | INTEGER | Touched on edit |
| `remind_at` | INTEGER, nullable | When the note should resurface as a nudge |
| `due_at` | INTEGER, nullable | The actual deadline, distinct from `remind_at` |
| `recurrence` | TEXT, nullable | RRULE-style string for repeating notes |
| `archived` | INTEGER, boolean | Hidden from active views, still searchable. Default 0 |
| `archived_at` | INTEGER, nullable | Set when `archived` flips to 1, cleared on restore. Drives the 90-day purge |
| `space_id` | TEXT, nullable | Foreign key to `spaces.id`. Null means unfiled, shown in the Inbox view |
| `snooze_count` | INTEGER | Increments on each snooze. Default 0. See the open question in section 22 on whether it affects ordering |
| `pinned` | INTEGER, boolean | Pinned notes always show. Default 0 |

### Table: `spaces`
A Space is a project or an area of life. A note belongs to at most one Space.
| Column | Type | Notes |
|---|---|---|
| `id` | TEXT, UUID | Primary key |
| `name` | TEXT | Display name |
| `color` | TEXT, nullable | Accent color; notes in the Space inherit it visually |
| `north_star_note_id` | TEXT, nullable | Foreign key to a note that describes the project, pinned at the top of the Space |
| `archived` | INTEGER, boolean | Archiving a Space sweeps its notes into the archive. Default 0 |
| `created_at` | INTEGER | |

### Table: `tags`
Flat, cross-cutting labels. Lowercase-normalized.
| Column | Type | Notes |
|---|---|---|
| `id` | TEXT, UUID | Primary key |
| `name` | TEXT, unique | Normalized, lowercase |

### Table: `note_tags`
| Column | Type | Notes |
|---|---|---|
| `note_id` | TEXT | Foreign key, part of composite key |
| `tag_id` | TEXT | Foreign key, part of composite key |

### Table: `attachments`
References to context, not copies, with one exception. See section 13.
| Column | Type | Notes |
|---|---|---|
| `id` | TEXT, UUID | Primary key |
| `note_id` | TEXT | Foreign key |
| `kind` | TEXT | `image_copied`, `file_ref`, `folder_ref`, or `url` |
| `path_or_url` | TEXT | Filesystem path or URL. For `image_copied`, a path inside app storage |
| `display_name` | TEXT, nullable | Friendly label |
| `added_at` | INTEGER | |

### Table: `saved_filters`
| Column | Type | Notes |
|---|---|---|
| `id` | TEXT, UUID | Primary key |
| `name` | TEXT | Display name in the sidebar |
| `query` | TEXT | Serialized filter conditions. See section 9 |
| `feeds_briefing` | INTEGER, boolean | If set, this filter produces its own mini-briefing. Default 0 |

### Indexes
Index `remind_at`, `due_at`, `space_id`, and `archived` on `notes`. Index `note_tags.tag_id` and `tags.name`. These keep the briefing query and all filtering instant at several thousand notes.

### Note on completion and derived states
There is no `status` column and no workflow state in the core, per an explicit decision to remove kanban from the base design. Completion is handled by `archived`: marking a note done sets `archived = 1`, which hides it from active views, the List, Stream, and Briefing, while keeping it searchable and able to resurface if a reminder or due date on it comes around or the user restores it. "Scheduled" is derived from `remind_at` being in the future rather than stored. The aged "someday" briefing bucket is computed as unfiled notes with no due date older than a threshold. There is no separate `state` column and no separate "completed" state distinct from archived.

### Automatic purge of old archived notes
Archived notes are permanently deleted once they have been archived for 90 days or more, configurable via `ARCHIVE_PURGE_DAYS`, default 90. To make this measurable, the `notes` table carries an `archived_at` timestamp set when `archived` flips to 1 and cleared when a note is restored; the purge compares `archived_at` against now. The purge runs at app launch, not on a background timer, consistent with the no-daemon model. Because this is unattended permanent deletion of user data, it must be conservative: only ever purge notes that are both archived and past the threshold, never an unarchived note, and never a pinned note even if archived. This behavior must be stated plainly in the README and be visible in Settings so it never surprises anyone.

---

## 7. Organization: Spaces and Tags

Two light layers, chosen so that real life fits without nesting.

**Spaces** answer the question "what project is this part of." A note lives in at most one Space. The capture default is no Space, which lands the note in the Inbox view, so the user never has to decide at capture time. Filing into a Space is a later, optional act. A Space can carry a color that its notes inherit, can hold a pinned north star note that describes the project, and can be archived as a whole when the project ends, which sweeps its notes into the archive while keeping them searchable forever. A Space is a lightweight area of life or work, for example Work, Infrastructure, Health, Learning, Personal, or Ideas. It is deliberately not a workspace, team, tenant, or isolated system, and should never feel like one.

**Tags** answer the question "what kind of thing is this." They cut across Spaces for themes such as idea, waiting, urgent, or someday. Tags are added inline by typing a hashtag in the note body, parsed the same way dates are, with autocomplete so they do not sprawl into near duplicates. A note can carry several tags but still lives in only one Space. A tag view shows every note with that tag across all Spaces.

The reason for two layers rather than one: a Space is a place, a tag is a quality. Forcing both jobs onto tags alone makes the system mushy; forcing both onto folders makes it rigid.

---

## 8. Prioritisation and Resurfacing Engine

There is no background loop. Resurfacing is a single computation run on demand at the trigger moments in section 10. Notes are ranked by a computed **date-driven score**, and the briefing shows the top of that ranking.

### The score
Ordering is date-driven. There is no manual priority field; per an explicit decision, position in the list is computed from time-based signals rather than set by hand. Exact weights are a tunable knob to be set after living with the app for a week.
- **Due proximity and lateness.** This is the primary driver. Overdue notes score high and rise further the more overdue they are. Notes due soon score moderately. Notes with no date sit below dated ones.
- **Two further signals are proposed but not yet confirmed; see the open question in section 22:** whether each snooze should nudge a note up the order, and whether undated notes should gently rise as they age. Both are time-related, but neither is settled, so they are flagged rather than assumed.

### Briefing buckets
Given `now`, surfaced notes are grouped and ordered:
| Bucket | Condition | Order |
|---|---|---|
| Overdue | `due_at < now` and not archived | by score, highest first |
| Due today | `due_at` within today | by score |
| Nudges | `remind_at <= now` and `remind_at` was in the future when set | by score |
| Aged someday | unfiled, no `due_at`, `created_at` older than `SOMEDAY_AGE_DAYS`, default 30 | oldest first |
| Pinned | `pinned = 1` | manual |

An empty briefing shows a calm empty state, not a blank or flashing screen.

### Decay and honesty
When `snooze_count` reaches `DECAY_THRESHOLD`, default 3, the card shows a gentle prompt such as "snoozed 3 times, still relevant? Reschedule or Archive". This stops the app from becoming a graveyard of stale reminders.

### Recurrence
When a recurring note fires, it resurfaces and the next occurrence is computed from `recurrence`.

---

## 9. Filtering and Saved Views

Filtering is a first-class surface, because past a few hundred notes it is the difference between a usable app and a prettier version of an endless scroll.

### Filter bar
A stacked filter bar sits on top of any list view: the List, the Stream, and any Space. Conditions narrow the set together. Supported conditions: tags, Space, whether a due date exists and its range, and a text match.

### Tag matching, all versus any
Tag filtering supports both combinations, because both are needed once tags pile up:
- **All:** notes carrying every listed tag, for example both work and urgent. Narrows hard.
- **Any:** notes carrying any listed tag, for example idea or someday. Casts wide.

### Inline filter language
A single search box understands the filter language as you type, so `#work #urgent due:thisweek` builds the filter without opening a panel. This reuses the same parsing approach as dates and hashtags elsewhere in the app.

### Saved views
When the same filter is built repeatedly, it can be saved with a name and becomes a one-click view in the sidebar. A saved view and the briefing are the same machinery pointed at different questions: the briefing asks "what is important right now" using the score, a saved view asks "show me this slice" using conditions. A saved view can optionally feed its own mini-briefing via `feeds_briefing`, so a view such as "side project, has a due date" can resurface its own top items.

### Optional kanban lens
A filtered list can optionally be displayed as a kanban board. This is a secondary view mode, never the default and never the core mental model, which stays a single prioritised list. Because the core has no workflow status field, the dimension the kanban columns group by is not yet decided. This is flagged as an open question in section 22 rather than resolved here, since resolving it would mean inventing a field the author has not asked for.

---

## 10. Runtime Loop

The app is always resident. Closing the window closes it to the tray; it never actually quits except via an explicit Quit in the tray menu. This is a hard requirement, not a preference, because the app must keep running to listen for external capture events. See section 16. The process owns all database writes; nothing else touches SQLite directly.

The moments that define the experience:
1. **Launch, then Briefing.** Compute the ranked briefing, show curated cards, the user dismisses, reschedules, or marks done, then the window closes to the tray.
2. **Tray click, then re-briefing.** Same computation, manual trigger, restores the window.
3. **Window focus.** Recompute the briefing, so an always-on PC stays current without rebooting.
4. **Global hotkey, then Capture.** A tiny always-on-top input box appears, the user types, Enter saves, the box vanishes. No window switch.
5. **External trigger, then Capture.** A Stream Deck button or any local client signals the resident app over the named pipe, which opens the same capture overlay as the hotkey.

After the briefing is handled, the app lives in the system tray with its IPC listener active. It does not stay a foreground window and it does not exit on window close.

### Tray behavior and menu
The tray icon is a first-class part of the product, behaving like a lightweight Windows utility in the spirit of Discord, PowerToys, ShareX, or OneDrive. Closing the main window minimizes to tray, preserves resident behavior, and keeps quick capture available globally. The tray menu exposes: Capture note, Open Tideline, Briefing, The List, Stream, Spaces, Settings, Check for updates, and Quit. Quit is the only action that actually exits the process.

---

## 11. Views and Screens

### 11.1 Briefing, the primary launch screen
Opens on launch, tray click, and focus. A vertical list of cards grouped by the buckets in section 8, with section headers. Each card shows the body text, a plain factual framing line such as "written 11 days ago" or "reminder for today", any due or remind chips, the Space color if filed, and actions: Done which archives the note, Snooze with quick options of plus one hour, tonight, tomorrow, next week, or custom, Edit, and Reschedule. The framing is always factual and never speculates about what the user was doing. See section 23. Curated, never a wall of everything. The target is a ten-second glance.

### 11.2 The List, the core view
A single straight list of notes ordered by the date-driven score in section 8, read top to bottom. This is the heart of the app and the better version of the message-to-self channel: one prioritised note stream, a ranked operational memory feed, never a spreadsheet, kanban, table, or task database. Cards stay note-like and low-density: content first, then light temporal framing, then optional context, then actions, then metadata last. Avoid dense metadata rows, heavy management actions, and visible workflow controls. The filter bar from section 9 sits on top, and the list can be narrowed by tags, Space, or date. Marking a note done archives it and it leaves the list.

### 11.3 Stream
A chronological feed of every note, newest at the bottom, reading like a raw chat log with yourself. This mirrors the private-channel habit the app replaces and is intentionally less curated than the Briefing: raw chronological memory, operational history, temporal continuity. It should resemble a messaging timeline or a personal log, not a SaaS activity feed or an issue tracker. It differs from the List only in ordering: the Stream is by time, the List is by score. It is the same notes, another lens. The Stream can be filtered by Space.

### 11.4 Space view
A single Space, showing only its notes as a prioritised List or a chronological Stream scoped to that Space, plus its own mini-briefing of just that project's resurfacing notes. The north star note is pinned at the top.

### 11.5 Optional kanban lens
Any filtered list can be flipped into a kanban view as a secondary, opt-in mode. It is never the default. The grouping dimension for its columns is an open question in section 22, because the core has no status field. Until that is decided, this view is not built.

### 11.6 Capture overlay
Triggered by the global hotkey, default `Ctrl+Alt+N`, or by the Stream Deck. A minimal borderless input, always on top, auto-focused. Inline natural-language date parsing pre-fills `remind_at` when the text contains a phrase such as "next Friday". Inline hashtags become tags. Enter saves, Escape cancels, both dismiss instantly. Capture never requires choosing a Space.

### 11.7 Settings
Modeled on the PowerToys settings layout. Hotkey rebind, auto-start toggle and launch delay, `SOMEDAY_AGE_DAYS` and `DECAY_THRESHOLD`, score weight tuning, theme of light, dark, or system, and the update channel.

---

## 12. Natural-Language Date Parsing
On capture and edit, scan `body` for date and time phrases such as tomorrow, next Friday, in 3 days, every Sunday, or May 12, using `Microsoft.Recognizers.Text.DateTime`. Recurring phrases populate `recurrence` and set a future `remind_at`. The parsed phrase is offered as a chip the user can confirm or reject; the note text is never silently rewritten.

---

## 13. Attachments and References

The default is to store a **reference**, a path or a URL, not a copy of the bytes. This keeps the database small, keeps the app fast and local-first, and matches the real intent of pointing at files and folders already on the PC.

Rules:
- Files and folders on the PC are stored as `file_ref` or `folder_ref` paths. Clicking opens them in Explorer or the default app.
- Links are stored as `url`.
- A reference can break if the user moves or renames the target. The app detects a dead link and shows a calm "this file moved" state rather than failing.
- The one exception is pasted images, which have no original location. Small pasted images are copied into app storage as `image_copied`. Larger pasted images are referenced if they came from a path.

---

## 14. Auto-Start
Use Task Scheduler with the trigger "at log on of the current user" and a delayed start of 8 seconds, configurable. Do not use the `HKCU` Run registry key, which fires too early and makes the app fight other startup programs for boot resources. The scheduled task launches the app minimized to the tray, which computes the briefing and shows it only if it is non-empty, otherwise stays silent in the tray. The settings toggle creates or deletes this task programmatically.

---

## 15. Notifications
Native Windows toasts for nudges found during a briefing computation. When several items need attention, show a single summary toast such as "3 notes need attention" that opens the briefing when clicked. Never one toast per note.

---

## 16. Stream Deck Integration and Local IPC

The app exposes a small local capture channel so external tools can trigger captures into the running instance. This is what makes the always-resident lifecycle in section 10 mandatory.

### 16.1 IPC channel, a named pipe
The resident app hosts a Windows named pipe, for example `\\.\pipe\tideline`, as its capture listener, using `System.IO.Pipes`. It is chosen over a localhost HTTP server because it opens no TCP port, so there is no firewall prompt, and the OS enforces which user and processes may connect. The pipe accepts line-delimited JSON, minimally `{"cmd":"capture"}` to raise the capture overlay, and reserves `{"cmd":"capture","text":"..."}` for future preset notes so the plugin never needs rewriting. The listener runs off the UI thread and marshals messages onto it. Malformed messages are ignored, not fatal.

### 16.2 Capture behavior for version 1
The Stream Deck button opens the capture overlay to type. It does not send canned text. It is effectively the global hotkey by proxy: press the button, the plugin sends `{"cmd":"capture"}`, the app raises the same overlay from section 11.5, the user types, Enter saves. Identical code path to the hotkey.

### 16.3 Stream Deck plugin
A standard Stream Deck plugin, which the Stream Deck SDK runs as a small JS or HTML process. Its single action, "Capture to Tideline", connects to the named pipe and sends the capture command on key press. If the app is not running, the plugin launches it, then retries once. Ship the plugin as a `.streamDeckPlugin` bundle alongside the app releases so installing it is one double-click. The plugin holds no state and no database access; it only signals the app, preserving the single-writer rule.

### 16.4 Future-proofing
Because the channel is generic, anything local, such as a phone-shortcut bridge or another macro tool, can capture into Tideline the same way. The named pipe is the one documented entry point.

---

## 17. Edge Cases and Rules
- **Immutable `created_at`.** The temporal framing depends on it; never overwrite it.
- **PC never reboots.** Covered by the tray-click and focus recompute in section 10.
- **Clock changes and time zones.** Store all times as UTC milliseconds, render local.
- **Empty briefing.** Show a calm empty state, do not flash and close.
- **Large note count.** The briefing query and filters are indexed and bucketed; the List and Stream paginate or virtualize.
- **Crash safety.** Writes commit per action to SQLite; there is no unsaved in-memory queue.
- **Hotkey conflict.** If global hotkey registration fails, surface a settings warning rather than failing silently.
- **App not running on Stream Deck press.** The plugin launches the app, then retries the pipe once.
- **Single instance.** Only one Tideline process may own the pipe and the database. A second launch hands off to the existing instance, raises its window, and exits.
- **Dead attachment reference.** Show the "this file moved" state, never crash.
- **Archiving a Space.** Archives its notes too, all still searchable.

---

## 18. Open Source and Distribution

The project is public and meant to be usable by anyone, so the repo needs the connective tissue an open source project requires, not just source code.

### 18.1 License
Ship a permissive license: MIT for simplicity, or Apache 2.0 for an explicit patent grant. Add a top-level `LICENSE` file and reference it in the README.

### 18.2 Repo essentials
A `README.md` with what Tideline is, a screenshot or short clip of the Briefing and Capture, a download link to the latest release, the SmartScreen "run anyway" note from section 19, and Stream Deck plugin setup. A `CONTRIBUTING.md` with build steps, the .NET and Windows App SDK requirements, branch and PR conventions, and the tagging release flow. A `CHANGELOG.md` or reliance on generated release notes. Issue templates.

### 18.3 Privacy posture, a feature not a footnote
Tideline is local-first: data lives only in the user's SQLite file, with no telemetry, analytics, or accounts. The only outbound call is the opt-in update check. State this plainly in the README, because for a notes app, "your data never leaves your machine" earns real trust.

### 18.4 Multi-user reality
Each user runs their own install; data and config live per user under `%LOCALAPPDATA%`. Do not bake in any author-specific paths, hardcoded names, or assumptions. The named pipe, hotkey, and database path must all resolve per user and per machine.

---

## 19. Release Pipeline and Self-Update

The repository is public, which removes auth friction: release assets are directly downloadable, so no token is embedded in the binary and no separate releases repo is needed.

### 19.1 CI build on release, GitHub Actions
Use **Velopack** to build the installer and update artifacts for the WinUI 3 app, driven by GitHub Actions. Trigger on a version tag, for example `v1.2.3`, not on every push, so ordinary commits do not build. Workflow steps: checkout, set up the .NET SDK, `dotnet publish` the WinUI 3 app self-contained for win-x64, run the Velopack pack step to produce the installer and update feed, then create a GitHub Release for the tag and upload the installer, the update artifacts, and the `.streamDeckPlugin` bundle. Developer release flow: bump the version, tag, push the tag, and CI does the rest.

### 19.2 Self-update on launch
Velopack handles the update check and the self-replace, which avoids the running-executable-cannot-overwrite-itself problem on Windows. On startup the app checks the latest GitHub Release, compares versions, and if newer downloads and stages the update, applying it on the next restart. The check must never block launch: if the network is down or the check fails, the app starts normally. Update is best effort, not a gate. Check once per launch, debounced so it does not re-check on every tray click within a session.

### 19.3 Code signing
Unsigned binaries trigger a SmartScreen "unknown publisher" warning on download or first run. For a publicly distributed app this hurts adoption, since some users abandon at the warning. Options, cheapest first: ship unsigned for version 1 with a clear README note on how to proceed past the warning; use free signing for open source projects via a service such as SignPath if eligible; or buy an OV or EV certificate at roughly 100 to 300 per year, where EV builds SmartScreen reputation fastest but costs most. Recommendation: launch unsigned, document the warning honestly, and pursue free open source signing once the project has traction. Track this as a hardening step, not a version 1 blocker.

---

## 20. Calendar Integration, Deferred

Calendar integration is out of version 1, but the time model should be designed so it can be added later through a clean boundary.

The intended direction is deliberately narrow and is now clarified: calendar integration is **one-way and reminder-oriented**, never bidirectional sync. Tideline desktop is always the source of truth; a calendar is only a reminder mirror. The app pushes due and remind times outward so they can raise notifications on a phone, a lock screen, or elsewhere in the user's ecosystem. The app must never treat the calendar as canonical storage, never sync full note content through it, and never let calendar edits flow back as the authority. The point is ecosystem convenience for reminders, nothing more.

The platform caveat, written down so it does not ambush anyone later: Apple Calendar is macOS and iOS only, and this app is Windows only, which is the decision that justifies the whole WinUI stack. A Windows app cannot talk to Apple Calendar directly. Because the desired integration is only a one-way reminder push, the realistic mechanism from Windows is to write events over CalDAV or iCloud, or to publish a subscribable calendar feed that Apple Calendar or Google Calendar reads. A one-way push is meaningfully simpler than the two-way sync considered earlier, so the Apple target is more feasible here than it first appeared; Google Calendar remains an easier API if the ecosystem does not matter.

Recommendation: keep calendar out of version 1, build the internal time model well first, and design the due-date and reminder system behind a clean interface that emits reminder events outward, so a one-way mirror to Apple or Google Calendar can be added later without touching the core.

---

## 21. Suggested Build Milestones
1. **Skeleton.** WinUI 3 app with Mica backdrop and the PowerToys-style navigation shell, SQLite schema, tray icon, close-to-tray, single instance.
2. **Capture.** Global hotkey, the overlay, save a bare note. No dates yet.
3. **The List and Stream.** The core prioritised List and the chronological Stream, edit, archive, full-text search.
4. **Time model.** Add `remind_at`, `due_at`, `recurrence`, reschedule and snooze.
5. **Organization.** Spaces and Tags, the Inbox default, inline hashtags, Space colors.
6. **Briefing and scoring.** The ranked briefing query, the date-driven score, buckets, the empty state.
7. **Enrichment.** Checklists and attachments as references.
8. **Filtering and saved views.** The filter bar, the inline filter language, all-versus-any tags, saved views with optional mini-briefings.
9. **Auto-start.** Task Scheduler with the delayed trigger and the settings toggle.
10. **IPC and Stream Deck.** The named-pipe listener reusing the capture overlay, plus the `.streamDeckPlugin` bundle.
11. **Release pipeline.** Velopack plus GitHub Actions on version tags, and self-update on launch.
12. **Open source scaffolding.** LICENSE, README with screenshots and the install note, CONTRIBUTING.
13. **Polish.** Fluent motion, theming, accent integration, summary toasts.
14. **Optional kanban lens.** Build only after the grouping question in section 22 is decided.

---

## 22. Open Decisions

These are genuinely undecided. They are listed as questions, not filled with a guess. Nothing here should be built until the author decides.

- **Kanban grouping dimension.** The optional kanban lens needs columns to group by, but the core has no status field, by decision. Open question: what should the columns be? Options include grouping by a chosen tag, by Space, or by due-date buckets such as overdue, today, this week, later. Not decided.
- **Non-date inputs to ordering.** Ordering is date-driven. Open question: beyond due-date proximity and lateness, should each snooze nudge a note up the order, and should undated notes gently rise as they age? Both are time-related, but neither is confirmed.
- **Completion model.** Done currently means archived, since there is no status field. Open question: is "done equals archived" the desired behavior, or should there be a distinct completed state that is not the same as archived? Not decided.

---

## 23. Copy and Tone, Non-Functional but Important

Tone is a hard product constraint, not decoration, because the wrong voice breaks the calm, low-pressure identity in section 1. The voice is factual, restrained, calm, concise, and believable. It must avoid AI or assistant language, coaching or self-help wording, and any productivity-guru tone. The app must not roleplay emotional intelligence or claim knowledge it does not have, per section 1.3.

Concretely, framing lines state plain facts about time and nothing more.

Good copy:
- "Written yesterday."
- "Written 11 days ago."
- "Reminder for today."
- "Reminder for tomorrow."

Copy to avoid:
- "Your past self wanted you to remember this." Speculative and sentimental.
- "You wrote this while working on infrastructure." Claims knowledge the app does not have.
- Anything that coaches, congratulates, or interprets the user's behavior.

The remaining principles still hold: capture must be instant or people stop using it; the briefing must stay curated or it becomes a wall of everything again; enrichment must always be optional or the app loses its calm; the user must never feel they are maintaining a system. These principles outrank any individual feature.

---

## 24. Future Mobile Companion, Out of Scope for Now

Tideline is fundamentally desktop-native, and the product must not be redesigned around mobile-first constraints. The desktop app is the source of truth. If a mobile app is ever built, it is a companion only: lightweight, focused on quick capture and on receiving reminders and resurfacing, and explicitly not the primary environment. This is recorded as direction so the desktop design is never compromised for a hypothetical phone app.
