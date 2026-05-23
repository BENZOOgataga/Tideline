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
            // Compact + bottom-right placement reads more like a real tray menu.
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Top,
        };

        // Quick actions
        menu.Items.Add(BuildItem(
            text: "Capture note",
            glyph: "",         // Edit pencil
            accelerator: "Ctrl+Alt+N",
            isAccent: true,
            onClick: _app.TriggerCapture));
        menu.Items.Add(BuildItem(
            text: "Open Tideline",
            glyph: "",         // OpenInNewWindow
            onClick: _app.ShowMainWindow));

        menu.Items.Add(new MenuFlyoutSeparator());

        // Navigation
        menu.Items.Add(BuildItem("Briefing", "", _app.ShowMainWindow));   // CalendarDay
        menu.Items.Add(BuildItem("The List", "", _app.ShowMainWindow));   // ViewAll
        menu.Items.Add(BuildItem("Stream",   "", _app.ShowMainWindow));   // Message
        menu.Items.Add(BuildItem("Spaces",   "", _app.ShowMainWindow));   // FolderHorizontal
        menu.Items.Add(BuildItem("Settings", "", _app.ShowMainWindow));   // Settings

        menu.Items.Add(new MenuFlyoutSeparator());

        menu.Items.Add(BuildItem(
            text: "Check for updates",
            glyph: "",         // CloudDownload
            onClick: null));

        menu.Items.Add(new MenuFlyoutSeparator());

        menu.Items.Add(BuildItem(
            text: "Quit",
            glyph: "",         // Power
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
            Icon = new FontIcon { Glyph = glyph, FontSize = 14 },
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
            item.Click += (_, _) => onClick();
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
