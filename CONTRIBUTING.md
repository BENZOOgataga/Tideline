# Contributing to Tideline

Thanks for thinking about contributing. Tideline is built primarily for the author, but it is published openly so anyone can use, fork, or improve it. This file explains what the project is willing to accept, how to set up a build, and how releases happen.

Please also read the [Code of Conduct](CODE_OF_CONDUCT.md). For anything security-related, follow [SECURITY.md](SECURITY.md) instead of opening a public issue.

---

## What kind of contributions fit

Tideline has strong opinions, and they are recorded in [`docs/plans/SPEC.md`](docs/plans/SPEC.md). Please skim it before opening a non-trivial PR.

**Welcome:**

- Bug fixes with a clear repro.
- Performance work backed by a measurement.
- Accessibility fixes (system font scaling, screen reader labels, keyboard navigation).
- Native polish that aligns with the WinUI 3 / PowerToys look (Mica, accent, Fluent motion).
- Documentation fixes.
- Build, packaging, and CI improvements.
- New translations, once the localisation story exists.

**Will likely be declined unless discussed first:**

- Features outside the SPEC's goals, or in its non-goals (cloud sync, mobile, accounts, nested folders, rich-text editor, AI features in v1, calendar grid view).
- Changes that break the **capture is instant, enrichment is optional later** rule.
- Adding telemetry, analytics, account systems, or non-essential outbound network calls.
- Cross-platform ports. Tideline is intentionally Windows-only; the entire stack choice depends on it.
- Large refactors with no concrete user-facing benefit.

If you are not sure, **open an issue first** and describe what you want to change and why. A short conversation up front saves a long PR conversation later.

---

## Reporting bugs

Use the **Bug report** issue template. Helpful bug reports include:

- Tideline version (Settings or the About surface).
- Windows version (`winver`).
- Exact steps to reproduce.
- What you expected vs what happened.
- Logs if the app produced any, screenshots or a short clip if the bug is visual.

If the database is involved, do **not** attach `notes.db` publicly. It is your data. Describe the schema state instead, or share privately if the maintainer asks.

## Suggesting features

Use the **Feature request** template. Tie the suggestion back to one of the SPEC's goals, or argue clearly why a new goal is worth adopting. "It would be nice if..." is a fine start, but the question that decides it is whether the feature makes Tideline calmer and more focused, or busier.

---

## Development setup

### Prerequisites

- **Windows 10 1809+** or **Windows 11**, x64.
- **.NET 8 SDK** ([download](https://dotnet.microsoft.com/download/dotnet/8.0)).
- **Windows App SDK** for WinUI 3 development. The Visual Studio installer can provision this; otherwise see the [official docs](https://learn.microsoft.com/windows/apps/windows-app-sdk/set-up-your-development-environment).
- **Visual Studio 2022** (Community is fine) with the workloads:
  - .NET desktop development
  - Windows application development
  - **Windows App SDK C# Templates** (individual component)
- Optional: **Stream Deck SDK** if you intend to touch the plugin.

### Get the source

```powershell
git clone https://github.com/BENZOOgataga/Tideline.git
cd Tideline
```

### Build

From Visual Studio: open the solution, set the WinUI 3 app project as startup, and `F5`.

From the CLI:

```powershell
dotnet restore
dotnet build -c Debug
```

### Run

```powershell
dotnet run --project src\Tideline.App
```

(Project path will be confirmed once the skeleton lands; see milestone 1 in the SPEC.)

### Tests

```powershell
dotnet test
```

If you fix a bug, add a test that fails before your fix and passes after. If you add a feature, add at least one test for the happy path and one for an edge case.

---

## Coding conventions

- **Language:** C# 12 on .NET 8.
- **Style:** the repo's `.editorconfig` is the source of truth. Run `dotnet format` before committing.
- **Nullable reference types:** enabled. Do not suppress warnings without a comment explaining why.
- **No author-specific paths or hardcoded names.** All per-user state must resolve under `%LOCALAPPDATA%\Tideline`. The named pipe, hotkey, and database path must work for any user on any machine.
- **Single writer to SQLite.** Only the resident app process writes to the database. The Stream Deck plugin and any future IPC clients only signal the app over the named pipe.
- **Immutable `created_at`.** The past-self framing depends on it. Never overwrite it, in code or in migrations.
- **All times in UTC milliseconds in storage, rendered local.**
- **No background polling loops** for resurfacing. Resurfacing is a single computation run at the trigger moments in SPEC section 10.
- **No telemetry, analytics, or accounts.** The only outbound call is the opt-in update check.
- **Document conventions for prose:** the SPEC uses only straight quotes and basic punctuation, no em dash, en dash, ellipsis character, or smart quotes. Try to match this in user-facing strings and docs. Code, identifiers, and config syntax are exempt.

---

## Branch and PR conventions

### Branches

- `main` is the release branch. CI builds installers from tags on `main`.
- Work happens on short-lived feature branches off `main`.
- Naming: `feat/short-description`, `fix/short-description`, `docs/short-description`, `chore/short-description`.

### Commit messages

Conventional Commits, lowercase subject, imperative mood, no trailing period:

```
feat(briefing): add aged someday bucket
fix(hotkey): surface a settings warning when registration fails
docs(readme): add Stream Deck plugin install note
chore(ci): bump actions/checkout to v4
```

Common types: `feat`, `fix`, `docs`, `refactor`, `perf`, `test`, `chore`, `build`, `ci`.

Keep commits focused. One logical change per commit. Squash noise before opening the PR.

### Pull requests

- Open against `main`.
- Use the PR template. Fill in the **What** and **Why** sections.
- Link the issue the PR resolves (`Closes #123`).
- Keep PRs small. A PR that touches one concern is easy to review; a PR that touches five is not.
- All checks must pass.
- The maintainer will review when they can. Open source on a one-person project means review latency exists; please be patient.

---

## Release flow

Releases are triggered by **version tags**, not by every push to `main`. The flow:

1. Decide a version following [SemVer](https://semver.org): `MAJOR.MINOR.PATCH`.
2. Update [`CHANGELOG.md`](CHANGELOG.md): move items from **Unreleased** into a new dated section.
3. Bump the version in the project files.
4. Commit: `chore(release): v1.2.3`.
5. Tag: `git tag v1.2.3`.
6. Push: `git push && git push --tags`.
7. CI (GitHub Actions, on the tag) runs `dotnet publish` self-contained for `win-x64`, runs Velopack pack, creates the GitHub Release for the tag, and uploads the installer, update artifacts, and the `.streamDeckPlugin` bundle.

Self-update on user machines is handled by Velopack on next launch.

Only maintainers tag releases.

---

## Documentation changes

Docs PRs are very welcome. Notes:

- The SPEC at [`docs/plans/SPEC.md`](docs/plans/SPEC.md) is the design document. Treat it as load-bearing: changes there change the project's direction. Open an issue first.
- The README is the marketing and quick-start surface. Keep it scannable.
- This file (CONTRIBUTING) is process. Keep it accurate to how the project actually runs.

---

## Questions

Open a [Discussion](https://github.com/BENZOOgataga/Tideline/discussions) if you have a question that is not a bug and not a feature request. For bugs, open an issue. For private security reports, see [SECURITY.md](SECURITY.md).

Thanks for being here.
