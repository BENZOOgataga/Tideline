# Common issues and gotchas

Stuff already paid for. Read first when something feels weird; check at the end of a session whether you have a new entry to add.

Format: **Symptom -> Cause -> Fix (file:line if applicable)**.

## WinUI 3 XAML

### `Visibility="{x:Bind HasFoo}"` crashes the page with `COMException 0x80004005` in `MeasureOverride`
- **Cause.** WinUI 3 `x:Bind` does NOT auto-convert `bool` to `Visibility` like classic WPF. The first time a row tries to render, the binding fails inside `MeasureOverride`.
- **Fix.** Expose a `Visibility`-typed property on the view-model (e.g. `DueVisibility => HasDue ? Visibility.Visible : Visibility.Collapsed`) and bind to that. See `src/Tideline.App/ViewModels/NoteCard.cs`.

### `ListView` inside `ScrollViewer` crashes in `MeasureOverride` once items render
- **Cause.** `ListView` is itself a scrolling, virtualizing container. Wrapping it in a `ScrollViewer` gives the inner list an infinite measure height.
- **Fix.** Drop the outer `ScrollViewer`. Put the `ListView` directly in the page `Grid` row with `Height="*"`.

### `Style="{ThemeResource SubtleButtonStyle}"` builds but throws at render
- **Cause.** `SubtleButtonStyle` is a WinUI 2 / UWP resource, not WinUI 3.
- **Fix.** Use no style (default `Button`) or one of the actual WinUI 3 keys (`AccentButtonStyle`, `DefaultButtonStyle`).

### `DataContext="{x:Bind}"` in a `DataTemplate` builds but is fragile
- **Cause.** Empty-path `{x:Bind}` semantics changed between SDK versions; in WinUI 3 it can render with no path bound.
- **Fix.** Drop the binding. `MenuFlyoutItem` / `ListViewItem` already exposes the item as `DataContext` in templates.

### `Microsoft.UI.Color` does not exist in WindowsAppSDK 1.6
- **Symptom.** CS0234 `Color does not exist in namespace Microsoft.UI`.
- **Fix.** Use `Windows.UI.Color.FromArgb(...)`. `Microsoft.UI.Colors.<Named>` still works for the named brushes (`White`, `Black`, etc.).

### Initial keyboard focus paints a visible focus ring on launch
- **Cause.** NavigationView selects its first item and that item gets keyboard focus on first activation.
- **Fix.** Subscribe to first `Activated` on the window, then `DispatcherQueue.TryEnqueue(Low, () => ContentFrame.Focus(FocusState.Pointer))`. Set `UseSystemFocusVisuals="False"` on the Frame so the pointer-state focus is silent.

### Custom title bar leaves the close button stuck in red hover state
- **Cause.** `ExtendsContentIntoTitleBar=true` leaves the system caption buttons without a resolved background.
- **Fix.** Set the full `AppWindow.TitleBar.Button*Color` set (transparent normal, subtle white hover and press). See `src/Tideline.App/Views/MainWindow.xaml.cs`.

### Capture overlay paints a white system border on Windows 11
- **Cause.** Even with `OverlappedPresenter.SetBorderAndTitleBar(false, false)` and `DwmSetWindowAttribute(BORDER_COLOR=COLOR_NONE)`, Windows 11 still paints a 1px frame on the default overlapped presenter.
- **Fix.** Use `OverlappedPresenter.CreateForContextMenu()` for borderless popup-style windows. It has no NC frame at all and is what menus/overlays are supposed to use. See `src/Tideline.App/Views/CaptureWindow.xaml.cs`.

## Win32 / P/Invoke

### `RegisterHotKey` returns `1408 ERROR_WINDOW_OF_OTHER_THREAD`
- **Cause.** `RegisterHotKey` must be called on the same thread that owns the receiving window. Creating the window on a background thread but calling Register from the UI thread fails.
- **Fix.** Do the register inside the same thread's message loop, after the window is created but before pumping messages. See `src/Tideline.App/Services/HotkeyService.cs`.

### Capture overlay does not receive keyboard focus when raised by the hotkey
- **Cause.** Another app owns the foreground; OS denies focus transfer unless the request comes from the active thread.
- **Fix.** `AttachThreadInput` to the current foreground thread, call `SetForegroundWindow`, then `Focus(FocusState.Keyboard)`. Defer the actual `Focus` call to the next `DispatcherQueue.TryEnqueue(Low, ...)` tick so the TextBox has attached to the visual tree.

### PUA / private-use-area Unicode characters disappear when passed through text pipelines
- **Symptom.** Segoe Fluent / MDL2 glyphs like `` end up as empty strings in committed source.
- **Fix.** Declare glyphs in source via `char.ConvertFromUtf32(0xE104)` so the file stays plain ASCII and survives any sanitizing pipeline.

## SQLite + IPC

### `SqliteConnection` is NOT thread-safe
- **Symptom.** Random `Pipe is broken`, malformed reads, or NRE in capture/count flows triggered via the named pipe.
- **Fix.** All IPC-side database work must marshal to the UI dispatcher before touching `Host.Notes` or any repository. See `src/Tideline.App/Services/IpcListener.cs::MarshalToUi`.

### Named pipe server drops the client connection before it reads
- **Cause.** `using NamedPipeServerStream pipe = ...` inside the accept loop disposes the pipe as soon as the loop iterates, killing the in-flight client read.
- **Fix.** Hand ownership of the connected pipe to the handler task; let the handler's `finally` dispose it.

### Removing `WaitForPipeDrain` causes the client to read "Pipe is broken"
- **Cause.** Server writes -> closes pipe before client read flushes the kernel buffer.
- **Fix.** Keep `WaitForPipeDrain` but cap it with a 2-second cancellation so a stuck client cannot pin a thread-pool slot indefinitely.

## H.NotifyIcon (tray)

### `MenuFlyoutItem.Click` event never fires from the tray flyout
- **Cause.** The tray flyout hosts the menu in a window that does not bubble WinUI events into the dispatcher.
- **Fix.** Use `MenuFlyoutItem.Command` with an `ICommand` (e.g. the existing `RelayCommand`). Commands route correctly through the flyout host.

### Tray menu does not render icons / accelerator hints / weight
- **Cause.** The default `ContextMenuMode` for `TaskbarIcon` is `PopupMenu`, a native Win32 menu that ignores WinUI item chrome.
- **Trade-offs.**
  - `PopupMenu` is text-only but always opens on right click and never auto-dismisses on cursor leave.
  - `SecondWindow` renders full WinUI menus but the popup host auto-dismisses on cursor leave or focus change.
  - `ActiveWindow` needs an active main window; fails when the app is in the tray with no window shown.
- **Current choice.** `PopupMenu`. Reliability over chrome.

## Velopack updates

### `WindowsVelopackLocator` fails to initialise: "not installed or packaged properly"
- **Cause.** The workflow installs the latest `vpk` CLI on every run, but `Tideline.App.csproj` pins the Velopack NuGet to a specific version. A protocol mismatch between `Update.exe` and the in-process library leaves the locator unable to detect the install.
- **Fix.** Pin `dotnet tool install -g vpk --version 0.0.1298` (or whatever matches the NuGet) in `.github/workflows/release.yml`.

### `GithubSource` says "Could not find asset called 'releases.win.json' in GitHub Release ..."
- **Cause.** Velopack 0.0.1298 reads per-channel manifests (`releases.win.json`, `assets.win.json`) from each release, not just the legacy `RELEASES` file.
- **Fix.** Upload `releases/releases.*.json` and `releases/assets.win.json` alongside the existing artifacts in the release workflow.

### Settings -> About is stuck on "Checking..." after a manual check
- **Cause.** `PopulateAbout` was called via a single low-priority dispatch tick after the click; the async check had not finished yet.
- **Fix.** `UpdateService` raises `CheckCompleted` in its `finally` block; `SettingsPage` subscribes and re-runs `PopulateAbout` from the event.

## Auto-start

### Task Scheduler entries do NOT appear in Task Manager Startup
- **Cause.** Task Manager Startup reads `HKCU/HKLM\...\Run`, `RunOnce`, and `shell:startup`, NOT generic schtasks tasks.
- **Fix.** Use `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` with the exe launched as `<exe> --startup`. The app self-delays 8 seconds in `Program.Main` when `--startup` is present, mitigating the SPEC section 14 boot-fight concern that originally pushed us toward Task Scheduler.

### Task Manager toggle does not sync to the in-app switch
- **Cause.** Wrong interpretation of the StartupApproved blob bit.
- **Fix.** At `HKCU\...\Explorer\StartupApproved\Run\<Name>`, the binary blob's first byte has low bit SET when the user disabled the row in Task Manager. `0x02` / `0x06` = enabled, `0x03` = disabled. Code lives in `src/Tideline.App/Services/AutoStartService.cs::IsEnabled`.

### Task Manager Startup row shows "Tideline.exe" instead of "Tideline"
- **Cause.** Task Manager reads `FileDescription` from the apphost Win32 version block. `<AssemblyTitle>` only sets the managed `AssemblyTitleAttribute`, NOT the apphost FileDescription.
- **Fix.** Set `<Description>` in the csproj. MSBuild writes it into the apphost resource. Use conditional `PropertyGroup` to differentiate Debug ("Tideline Development") from Release ("Tideline").

## Task Scheduler XML

### `schtasks /XML` rejects the file: "ERROR: incorrect document syntax (1,2)"
- **Cause.** XML prolog declares `encoding="UTF-8"` but the file was written as UTF-8 (or vice versa). `schtasks` reads the bytes as UTF-16 LE by default.
- **Fix.** Write the file with `Encoding.Unicode` and declare `encoding="UTF-16"` in the prolog. Both sides must agree.

## Stream Deck plugin

### Custom plugin shows up but key press does nothing; property inspector loses settings
- **Cause.** Stream Deck SDK v2 (HTML CodePath) runs the plugin in a sandboxed Chromium WebView with no Node access. `require('child_process')` throws.
- **Fix.** Port the plugin to **Stream Deck SDK 6** with a `Nodejs` block in the manifest and a Node-side `bin/plugin.js`. See `tools/streamdeck-plugin/com.tideline.capture.sdPlugin/`.

### `.streamDeckPlugin` installs as empty / "Unable to install"
- **Cause.** The plugin's `bin/plugin.js` was caught by the .NET `bin/` ignore rule and never reached the bundle.
- **Fix.** Lift the ignore for `tools/streamdeck-plugin/com.tideline.capture.sdPlugin/bin/` in `.gitignore` (but keep the workflow-generated `tideline-capture.exe` ignored).

### Stream Deck plugin host strips MenuFlyoutItem chrome (not the same as tray, but related)
- See the tray section above for `ContextMenuMode` trade-offs.

## Git / publish

### Local branch tracks `origin/main` even though it is named `feat/something`
- **Cause.** Branch was created by renaming local `main`; rename preserves the upstream.
- **Risk.** If an extension auto-pushes on commit, commits land on `main` even though `git branch` shows `feat/...`.
- **Fix.** `git push -u origin feat/something:feat/something`, then `git branch --set-upstream-to=origin/feat/something feat/something`. Disable any "auto push on commit" setting in your git client.

### VS Code git extension silently pushes to remote on commit
- **Setting.** `git.postCommitCommand` set to `push` or `sync`.
- **Fix.** Set to `none` (or commit only from CLI) to avoid surprise pushes.

## Build / IDE

### .NET 8 SDK not on PATH but runtimes are
- **Cause.** Common on machines that have Visual Studio Build Tools without the SDK workload.
- **Fix.** Install the SDK via `winget install Microsoft.DotNet.SDK.8` (may hit "Another installation is in progress" 1618; retry once the previous MSI finishes).

### `dotnet test` works but `dotnet build src/Tideline.App` cannot copy the exe
- **Cause.** A `Tideline.exe` instance is running and holding the file.
- **Fix.** Stop the process first: `Get-Process Tideline -ErrorAction SilentlyContinue | Stop-Process -Force`.
