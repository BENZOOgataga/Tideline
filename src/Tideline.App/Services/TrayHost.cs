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
    // SymbolIcon enum values render in any WinUI host (including the
    // H.NotifyIcon popup), unlike FontIcon which sometimes loses its font
    // family when hosted off the main XamlRoot.

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
            // SecondWindow hosts the menu in a hidden WinUI window so
            // MenuFlyoutItem.Icon, KeyboardAcceleratorTextOverride and
            // FontWeight all render. The default PopupMenu mode is a
            // native Win32 menu (text-only).
            ContextMenuMode = H.NotifyIcon.ContextMenuMode.SecondWindow,
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
            symbol: Symbol.Edit,
            accelerator: "Ctrl+Alt+N",
            isAccent: true,
            onClick: _app.TriggerCapture));
        menu.Items.Add(BuildItem(
            text: "Open Tideline",
            symbol: Symbol.OpenWith,
            onClick: _app.ShowMainWindow));

        menu.Items.Add(new MenuFlyoutSeparator());

        menu.Items.Add(BuildItem("Briefing", Symbol.Calendar, _app.ShowMainWindow));
        menu.Items.Add(BuildItem("The List", Symbol.List,     _app.ShowMainWindow));
        menu.Items.Add(BuildItem("Stream",   Symbol.Message,  _app.ShowMainWindow));
        menu.Items.Add(BuildItem("Spaces",   Symbol.Folder,   _app.ShowMainWindow));
        menu.Items.Add(BuildItem("Settings", Symbol.Setting,  _app.ShowMainWindow));

        menu.Items.Add(new MenuFlyoutSeparator());

        menu.Items.Add(BuildItem(
            text: "Check for updates",
            symbol: Symbol.Download,
            onClick: null));

        menu.Items.Add(new MenuFlyoutSeparator());

        menu.Items.Add(BuildItem(
            text: "Quit",
            symbol: Symbol.Cancel,
            onClick: _app.QuitApp));

        return menu;
    }

    private static MenuFlyoutItem BuildItem(
        string text,
        Symbol symbol,
        Action? onClick,
        string? accelerator = null,
        bool isAccent = false)
    {
        MenuFlyoutItem item = new()
        {
            Text = text,
            Icon = new SymbolIcon(symbol),
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
            // ICommand binds through H.NotifyIcon's popup host;
            // MenuFlyoutItem.Click does not propagate from the tray flyout.
            item.Command = new RelayCommand(() =>
            {
                CrashLog.Write($"TrayClick[{text}]", new InvalidOperationException("clicked"));
                try { onClick(); }
                catch (Exception ex) { CrashLog.Write($"TrayClick[{text}].Run", ex); }
            });
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