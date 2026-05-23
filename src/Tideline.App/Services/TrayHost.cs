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
            // PopupMenu uses a native Win32 context menu. It always opens on
            // right-click, stays open until the user picks an item or clicks
            // elsewhere, and respects the tray-resident lifecycle (no main
            // window required). SecondWindow auto-dismissed on cursor leave;
            // ActiveWindow needed a visible main window.
            ContextMenuMode = H.NotifyIcon.ContextMenuMode.PopupMenu,
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
            MenuFlyoutPresenterStyle = BuildPresenterStyle(),
            AreOpenCloseAnimationsEnabled = false,
            ShouldConstrainToRootBounds = false,
            LightDismissOverlayMode = Microsoft.UI.Xaml.Controls.LightDismissOverlayMode.Off,
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

    private static Microsoft.UI.Xaml.Style BuildPresenterStyle()
    {
        // Force a hard width. The SecondWindow host does not resize to
        // content; MinWidth alone is ignored, so we set Width too and lift
        // the corner radius and padding to match Win11 system menus.
        Microsoft.UI.Xaml.Style style = new(typeof(MenuFlyoutPresenter));
        style.Setters.Add(new Microsoft.UI.Xaml.Setter(MenuFlyoutPresenter.WidthProperty, 300.0));
        style.Setters.Add(new Microsoft.UI.Xaml.Setter(MenuFlyoutPresenter.MinWidthProperty, 300.0));
        style.Setters.Add(new Microsoft.UI.Xaml.Setter(MenuFlyoutPresenter.MaxWidthProperty, 360.0));
        style.Setters.Add(new Microsoft.UI.Xaml.Setter(MenuFlyoutPresenter.PaddingProperty,
            new Microsoft.UI.Xaml.Thickness(0, 6, 0, 6)));
        style.Setters.Add(new Microsoft.UI.Xaml.Setter(MenuFlyoutPresenter.CornerRadiusProperty,
            new Microsoft.UI.Xaml.CornerRadius(8)));
        return style;
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
            // SecondWindow popup sizes from the first measure pass, before
            // the presenter style applies, so we force width per item too.
            MinWidth = 300,
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
                try { onClick(); }
                catch (Exception ex) { CrashLog.Write($"TrayClick[{text}]", ex); }
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