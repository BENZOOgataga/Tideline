# Tideline Repository Instructions

Use `docs/plans/SPEC.md` and `CONTRIBUTING.md` as the source of truth for product direction, architecture boundaries, and contribution policy.

This repository is for a native Windows desktop app built with .NET 8, WinUI 3, and the Windows App SDK. Do not assume a web app, Electron app, MAUI app, or cross-platform stack unless a human explicitly changes the project direction.

Preserve the core product rules from the spec:

- Capture must stay instant and must not require filing or extra decisions.
- Enrichment is optional and added later.
- The app is local-first.
- Do not add telemetry, analytics, accounts, cloud sync, or other non-essential outbound network calls.
- Do not introduce background polling loops for resurfacing.
- Keep per-user state under `%LOCALAPPDATA%\\Tideline`.
- Preserve the single-writer SQLite rule. External tools and plugins signal the app; they do not write to the database directly.

For prose in docs, comments, UI strings, PR text, and commit messages, match the character rule in `docs/plans/SPEC.md`: use only straight quotes and basic punctuation. Do not use em dashes, en dashes, the ellipsis character, or smart quotes. Code, identifiers, and config syntax are exempt.

When creating commits, use Conventional Commits only. Keep the subject lowercase, imperative, and without a trailing period. Do not add co-authors or `Co-authored-by` trailers.

Prefer small, scoped changes that align with the current scaffold and spec. If a request conflicts with the spec, call out the conflict clearly instead of silently drifting the project.
