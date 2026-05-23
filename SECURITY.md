# Security Policy

## Supported versions

Tideline is early-stage software. Security fixes are only guaranteed for the most recent release on `main`.

Before a stable `1.0.0` release, security fixes may land without backporting to older tags.

## Reporting a vulnerability

Please do **not** open a public GitHub issue for a security problem.

Instead, report it privately to the maintainer:

- GitHub: [@BENZOOgataga](https://github.com/BENZOOgataga)

Include:

- a clear description of the issue
- affected version or commit
- reproduction steps or a proof of concept
- impact assessment if known
- any suggested mitigation

If the issue involves local files, logs, or database content, redact private note data before sharing.

## What to expect

- An initial acknowledgement is targeted within 7 days.
- Triage will confirm whether the report is in scope and reproducible.
- If accepted, a fix will be prepared privately where practical, then released publicly.
- Credit can be given in release notes if you want it. If you prefer to stay unnamed, say so.

## Scope

In scope:

- vulnerabilities in the desktop app
- installer or update-path weaknesses
- IPC or local privilege boundary mistakes
- data exposure caused by the app

Out of scope:

- reports that require physical access to an unlocked machine with no app-specific weakness
- issues in third-party services or libraries with no exploitable impact through Tideline
- purely theoretical concerns with no realistic attack path
- general support requests or feature requests

## Privacy posture

Tideline is local-first. Security reports should preserve that posture. Do not publish private note content, `notes.db`, or personal paths in public threads.
