---
applyTo: "**"
---

# Tideline Repo-Wide Instructions

Use `docs/plans/SPEC.md` and `CONTRIBUTING.md` as the source of truth before making non-trivial code, doc, review, or commit decisions.

This is a Windows-only desktop app repository. Favor .NET 8, WinUI 3, Windows App SDK, SQLite, named-pipe IPC, Task Scheduler startup, Velopack packaging, and the open-source release flow already documented by the project. Do not assume a web app, browser-first architecture, or cross-platform stack unless explicitly directed to do so.

Preserve these product constraints:

- Capture is instant.
- Enrichment is optional and added later.
- The app is local-first.
- No telemetry, analytics, accounts, or cloud sync.
- No background polling loop for resurfacing.
- No author-specific paths or machine-specific assumptions.
- Only the resident app process writes to SQLite.

Match the repo's prose character rule in docs, comments, UI strings, review text, and generated commit messages:

- Use straight quotes and basic punctuation only.
- Do not use em dashes, en dashes, the ellipsis character, or smart quotes.
- Code, identifiers, and config syntax are exempt.

When generating a commit message, use Conventional Commits only. Keep the subject lowercase, imperative, and without a trailing period. Do not add co-authors or `Co-authored-by` trailers.

If a requested change pushes against the spec, say so plainly and keep the output aligned with the current project direction.
