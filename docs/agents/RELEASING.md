# Releasing

The release pipeline is fully tag-driven. **Agents do not initiate releases.** A release happens only when the user explicitly asks for one and the user has the GitHub permission to publish it (see [`../../CLAUDE.md`](../../CLAUDE.md) "Releases" section).

## What "release" actually does

Pushing a `v*` tag to `main` runs `.github/workflows/release.yml`, which:

1. Restores, tests (xunit Core suite must pass).
2. Publishes `src/Tideline.App` and `tools/Tideline.CaptureClient` self-contained for `win-x64`.
3. Copies `tideline-capture.exe` into the app publish dir so the install carries the helper.
4. Installs `vpk --version 0.0.1298` (must match the Velopack NuGet version pinned in `src/Tideline.App/Tideline.App.csproj`).
5. Runs `vpk pack`, producing `Tideline-win-Setup.exe`, `Tideline-<version>-full.nupkg`, `RELEASES`, `releases.win.json`, `assets.win.json`.
6. Bundles the Stream Deck plugin (`npm ci` inside the plugin folder, copies the helper exe in, zips as `Tideline.streamDeckPlugin`).
7. Creates the GitHub Release for the tag and uploads all of the above.

## How a user cuts a release

```powershell
# bump the line in release notes
git fetch origin
git checkout main
git pull
gh release create v0.1.X --target main --title "..." --notes "..."
```

`gh release create` creates the tag, which fires the workflow. No `git tag` + `git push --tags` ceremony required.

Watch the run:

```powershell
gh run watch <run-id>
gh run list --workflow=release.yml --limit 5
```

## Versioning

- Semver. Patch bumps for fixes and small additions, minor bumps for new functional surface, major bumps when an SPEC contract changes.
- Tag format: `v<major>.<minor>.<patch>`.
- `Directory.Build.props` carries a default `<Version>0.1.0</Version>` used only for local Debug builds. The release workflow passes `/p:Version=${{ steps.ver.outputs.version }}` so the published binaries carry the real version.

## Pre-flight checklist

Before asking the user to cut a release:
- [ ] All commits on `main`. (Brief says agents work on `main` directly now; the original `feat/v1-core` workflow ended at v0.1.0.)
- [ ] `dotnet test tests/Tideline.Core.Tests/` is green.
- [ ] Local Debug build of the app launches.
- [ ] `powershell -ExecutionPolicy Bypass -File tools/harness/Run-IpcSmoke.ps1` passes.
- [ ] [`COMMON_ISSUES.md`](COMMON_ISSUES.md) is updated with any new trap you paid for.
- [ ] If the change touches the update flow, the auto-start flow, or the release workflow itself, write a release-notes paragraph that explicitly calls it out so the user can re-test.

## After the release

- Confirm the workflow ran green (`gh run list --workflow=release.yml`).
- Confirm the release page shows the expected asset set:
  - `Tideline-win-Setup.exe`
  - `Tideline-<version>-full.nupkg`
  - `RELEASES`
  - `releases.win.json`
  - `assets.win.json`
  - `Tideline.streamDeckPlugin`
- If any assets are missing, the in-app update check will silently fall over. The most likely cause is a workflow YAML typo in the upload list.

## Known constraints

- Existing installs from before v0.1.6 had a broken Velopack locator (vpk version mismatch). They cannot self-update; users must reinstall once from `Tideline-win-Setup.exe`.
- The `vpk` CLI version and the `Velopack` NuGet version in the app csproj **must** match. Bumping either requires bumping the other in the same commit.
- The Stream Deck plugin requires Stream Deck app 6.5 or later. Plugins built against SDK 6 do not load on older Stream Deck app versions.

## Things to NEVER do

- Cut a release on agent initiative.
- Push a `v*` tag with `--force` or rewrite a release retroactively (clients pin to commits).
- Skip the test step in the workflow to ship faster.
- Use a different `vpk` version than the pinned `Velopack` NuGet.
