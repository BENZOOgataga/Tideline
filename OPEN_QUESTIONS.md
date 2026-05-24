# Open questions

Questions Tideline v1 does not answer. The agent did not silently guess
on any of these; they are listed for the owner to decide. Anything coded
as a stopgap that needs a real answer is also recorded here.

## From SPEC section 22 (unchanged by v1)

- **Kanban grouping dimension.** The optional kanban lens needs columns
  to group by, but the core has no status field, by decision. v1
  deliberately does not build the kanban lens (SPEC milestone 14).
- **Non-date inputs to ordering.** Whether each snooze should nudge a
  note up the briefing order, and whether undated notes should gently
  rise as they age. v1 implements only the date-driven score per SPEC
  section 8.
- **Completion model.** Whether "done equals archived" is the final
  behavior or a distinct completed state is wanted. v1 treats Done as
  Archive.

## Added by the v1 build

- **Velopack version pin.** [`src/Tideline.App/Services/UpdateService.cs`](src/Tideline.App/Services/UpdateService.cs)
  is a placeholder. The release workflow in
  [`.github/workflows/release.yml`](.github/workflows/release.yml)
  installs `vpk` as a global dotnet tool and packs against the published
  app, which is independent of the in-process update check. To enable
  in-process self-update, pick a Velopack NuGet version, add it to
  `Tideline.App.csproj`, call `VelopackApp.Build().Run()` very early in
  `Program.Main`, and replace the body of `UpdateService.CheckOnceFireAndForget()`
  with an `UpdateManager` check against the GitHub Releases feed.

- **Stream Deck plugin port to SDK 6.** The scaffold in
  [`tools/streamdeck-plugin/`](tools/streamdeck-plugin/) was written
  against the legacy Stream Deck SDK v2 (HTML / WebView), which runs in
  a Chromium sandbox with no Node access, so `plugin.js` cannot spawn
  `tideline-capture.exe`. The bundled `.streamDeckPlugin` therefore
  does not work as-is. Workaround that works today: use Stream Deck's
  built-in System -> Open action against `tideline-capture.exe` (see
  the plugin README). Permanent fix: port the plugin to Stream Deck
  SDK 6 (Node-based runtime) so plugin.js can actually invoke the
  helper.

- **Code signing.** Per SPEC section 19.3, v1 ships unsigned, with the
  README documenting the SmartScreen "Run anyway" path. Pick a signing
  story (free OSS via SignPath, or paid OV/EV cert) before broad
  distribution; the workflow does not currently sign.

- **Toast notifications.** SPEC section 15 wants a single summary toast
  when several items need attention. v1 does not yet emit toasts; the
  briefing UI carries this signal in-app. Add
  `Microsoft.Windows.AppNotifications` wiring under polish.

- **Inline natural-language date parsing.** SPEC section 12 wants
  `Microsoft.Recognizers.Text.DateTime` to extract phrases such as
  "tomorrow at 9am" from the capture body and offer a confirm chip. v1
  ships explicit DatePicker and TimePicker controls in
  [`NoteEditDialog.xaml`](src/Tideline.App/Views/NoteEditDialog.xaml)
  instead, plus the snooze quick buttons. The recognizer integration
  has not been added yet.
