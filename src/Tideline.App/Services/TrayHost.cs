using System;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;

namespace Tideline.App.Services;

/// <summary>
/// Owns the system tray icon and its menu. Built in code so we avoid carrying
/// a separate XAML host just for tray UI. The app must stay resident, so Quit
/// is the only menu item that actually exits the process.
/// </summary>
public sealed class TrayHost : IDisposable
{
    // Segoe Fluent Icons code points. Materialised via char.ConvertFromUtf32 so
    // the source file stays plain ASCII and survives any text pipeline that
    // strips Private Use Area glyphs.
    // https://learn.microsoft.com/windows/apps/design/style/segoe-fluent-icons-font
    private static readonly string GlyphEdit            = char.ConvertFromUtf32(0xE104);
    private static readonly string GlyphOpenInNewWindow = char.ConvertFromUtf32(0xE8A7);
    private static readonly string GlyphCalendarDay     = char.ConvertFromUtf32(0xE787);
    private static readonly string GlyphViewAll         = char.ConvertFromUtf32(0xE8FD);
    private static readonly string GlyphMessage         = char.ConvertFromUtf32(0xE8BD);
    private static readonly string GlyphFolder          = char.ConvertFromUtf32(0xE8B7);
    private static readonly string GlyphSetting         = char.ConvertFromUtf32(0xE713);
    private static readonly string GlyphCloudDownload   = char.ConvertFromUtf32(0xEBD3);
    private static readonly string GlyphPower           = char.ConvertFromUtf32(0xE7E8);

    private readonly App _app;
    private readonly TaskbarIcon _icon;
    private bool _disposed;

    public TrayHost(App app)
    {
        _app = app;
        _icon = new TaskbarIcon
        {
            ToolTipText = "Tideline",
            NoLeftClickDelay = true,
        };

        try
        {
            string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Tideline.ico");
            if (System.IO.File.Exists(iconPath))
            {
                _icon.IconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(iconPath));
            }
        }
        catch
        {
            // Tray will still render with a default if our icon fails to load.
        }

        _icon.ContextFlyout = BuildMenu();
        _icon.LeftClickCommand = new RelayCommand(() => _app.ShowMainWindow());
        _icon.DoubleClickCommand = new RelayCommand(() => _app.TriggerCapture());
        _icon.ForceCreate();
    }

    private MenuFlyout BuildMenu()
    {
        MenuFlyout menu = new()
        {
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Top,
        };

        menu.Items.Add(BuildItem(
            text: "Capture note",
            glyph: GlyphEdit,
            accelerator: "Ctrl+Alt+N",
            isAccent: true,
            onClick: _app.TriggerCapture));
        menu.Items.Add(BuildItem(
            text: "Open Tideline",
            glyph: GlyphOpenInNewWindow,
            onClick: _app.ShowMainWindow));

        menu.Items.Add(new MenuFlyoutSeparator());

        menu.Items.Add(BuildItem("Briefing", GlyphCalendarDay, _app.ShowMainWindow));
        menu.Items.Add(BuildItem("The List", GlyphViewAll,     _app.ShowMainWindow));
        menu.Items.Add(BuildItem("Stream",   GlyphMessage,     _app.ShowMainWindow));
        menu.Items.Add(BuildItem("Spaces",   GlyphFolder,      _app.ShowMainWindow));
        menu.Items.Add(BuildItem("Settings", GlyphSetting,     _app.ShowMainWindow));

        menu.Items.Add(new MenuFlyoutSeparator());

        menu.Items.Add(BuildItem(
            text: "Check for updates",
            glyph: GlyphCloudDownload,
            onClick: null));

        menu.Items.Add(new MenuFlyoutSeparator());

        menu.Items.Add(BuildItem(
            text: "Quit",
            glyph: GlyphPower,
            onClick: _app.QuitApp));

        return menu;
    }

    private static MenuFlyoutItem BuildItem(
        string text,
        string glyph,
        Action? onClick,
        string? accelerator = null,
        bool isAccent = false)
    {
        MenuFlyoutItem item = new()
        {
            Text = text,
            Icon = new FontIcon
            {
                Glyph = glyph,
                FontSize = 14,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets"),
            },
            IsEnabled = onClick is not null,
        };
        if (!string.IsNullOrEmpty(accelerator))
        {
            item.KeyboardAcceleratorTextOverride = accelerator;
        }
        if (isAccent)
        {
            item.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        }
        if (onClick is not null)
        {
            item.Click += (_, _) =>
            {
                CrashLog.Write($"TrayClick[{text}]", new InvalidOperationException("clicked"));
                try { onClick(); }
                catch (Exception ex) { CrashLog.Write($"TrayClick[{text}].Run", ex); }
            };
        }
        return item;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _icon.Dispose(); } catch { /* ignored */ }
    }

    private sealed class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action _action;
        public RelayCommand(Action action) => _action = action;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _action();
        public event EventHandler? CanExecuteChanged
        {
            add { } remove { }
        }
    }
}