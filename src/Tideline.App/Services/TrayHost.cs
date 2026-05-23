using System;
using H.NotifyIcon;
using H.NotifyIcon.Core;
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
        MenuFlyout menu = new();

        MenuFlyoutItem capture = new() { Text = "Capture note" };
        capture.Click += (_, _) => _app.TriggerCapture();
        menu.Items.Add(capture);

        MenuFlyoutItem open = new() { Text = "Open Tideline" };
        open.Click += (_, _) => _app.ShowMainWindow();
        menu.Items.Add(open);

        menu.Items.Add(new MenuFlyoutSeparator());

        MenuFlyoutItem briefing = new() { Text = "Briefing" };
        briefing.Click += (_, _) => _app.ShowMainWindow();
        menu.Items.Add(briefing);

        MenuFlyoutItem theList = new() { Text = "The List" };
        theList.Click += (_, _) => _app.ShowMainWindow();
        menu.Items.Add(theList);

        MenuFlyoutItem stream = new() { Text = "Stream" };
        stream.Click += (_, _) => _app.ShowMainWindow();
        menu.Items.Add(stream);

        MenuFlyoutItem spaces = new() { Text = "Spaces" };
        spaces.Click += (_, _) => _app.ShowMainWindow();
        menu.Items.Add(spaces);

        MenuFlyoutItem settings = new() { Text = "Settings" };
        settings.Click += (_, _) => _app.ShowMainWindow();
        menu.Items.Add(settings);

        menu.Items.Add(new MenuFlyoutSeparator());

        MenuFlyoutItem updates = new() { Text = "Check for updates", IsEnabled = false };
        menu.Items.Add(updates);

        menu.Items.Add(new MenuFlyoutSeparator());

        MenuFlyoutItem quit = new() { Text = "Quit" };
        quit.Click += (_, _) => _app.QuitApp();
        menu.Items.Add(quit);

        return menu;
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
