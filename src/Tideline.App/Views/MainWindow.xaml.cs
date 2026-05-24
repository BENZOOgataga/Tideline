using System;
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Tideline.App.Services;
using WinRT.Interop;

namespace Tideline.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly AppHost _host;

    public MainWindow(AppHost host)
    {
        _host = host;
        InitializeComponent();
        Title = Brand.DisplayName;
        AppTitleText.Text = Brand.DisplayName;

        string iconPath = Brand.IconPath;
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1240, 820));
        AppWindow.Closing += OnAppWindowClosing;

        // Custom title bar: extend client area, hand the drag region to
        // AppTitleBar. Windows still draws the min/max/close caption
        // buttons on the right; we just leave space for them.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Caption-button colours. Without these the system paints the close
        // button in a stuck "hover" red because it has no resolved
        // background to fall back to over our Mica content.
        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor              = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor      = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor         = Windows.UI.Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF);
        titleBar.ButtonPressedBackgroundColor       = Windows.UI.Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF);
        titleBar.ButtonForegroundColor              = Windows.UI.Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0);
        titleBar.ButtonInactiveForegroundColor      = Windows.UI.Color.FromArgb(0xFF, 0x80, 0x80, 0x80);
        titleBar.ButtonHoverForegroundColor         = Microsoft.UI.Colors.White;
        titleBar.ButtonPressedForegroundColor       = Microsoft.UI.Colors.White;

        string pngPath = Brand.PngPath;
        if (File.Exists(pngPath))
        {
            AppTitleIcon.Source = new BitmapImage(new Uri(pngPath));
        }

        Navigate("briefing");

        if (App.Current?.Updates is { } updates)
        {
            updates.UpdateReady += OnUpdateReady;
            if (updates.HasUpdate) ShowUpdateBanner(updates.AvailableVersion);
        }

        // Move initial focus off the NavigationView so the first selected
        // item does not paint a keyboard focus ring on launch. Frame is set
        // to UseSystemFocusVisuals=False so it absorbs focus silently.
        Activated += OnFirstActivation;
    }

    private void OnFirstActivation(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated) return;
        Activated -= OnFirstActivation;
        DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            () => ContentFrame.Focus(FocusState.Pointer));
    }

    private void OnUpdateReady()
    {
        ShowUpdateBanner(App.Current?.Updates?.AvailableVersion);
    }

    private void ShowUpdateBanner(string? version)
    {
        if (string.IsNullOrEmpty(version)) return;
        UpdateBanner.Message = $"Tideline {version} is ready to install. Restart to apply.";
        UpdateBanner.IsOpen = true;
    }

    private void UpdateInstall_Click(object sender, RoutedEventArgs e)
    {
        App.Current?.Updates?.ApplyAndRestart();
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // Close-to-tray: closing the window must not exit the process,
        // because the resident app owns the IPC listener and global hotkey.
        // The exception is when the user picked Quit from the tray menu;
        // App.IsShuttingDown flips first so this handler steps out of the way.
        if (App.Current?.IsShuttingDown == true)
        {
            return;
        }
        args.Cancel = true;
        sender.Hide();
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            Navigate(tag);
        }
    }

    private void Navigate(string tag)
    {
        try
        {
            NavigateInner(tag);
        }
        catch (Exception ex)
        {
            CrashLog.Write($"Navigate({tag})", ex);
            throw;
        }
    }

    private void NavigateInner(string tag)
    {
        switch (tag)
        {
            case "briefing":
                ContentFrame.Navigate(typeof(BriefingPage), _host);
                break;
            case "list":
                ContentFrame.Navigate(typeof(ListPage), new ListPage.NavArg(_host, null, IncludeUnfiledOnly: false));
                break;
            case "stream":
                ContentFrame.Navigate(typeof(StreamPage), _host);
                break;
            case "inbox":
                ContentFrame.Navigate(typeof(ListPage), new ListPage.NavArg(_host, null, IncludeUnfiledOnly: true));
                break;
            case "spaces":
                ContentFrame.Navigate(typeof(SpacesPage), _host);
                break;
            case "saved":
                ContentFrame.Navigate(typeof(SavedViewsPage), _host);
                break;
            case "settings":
                ContentFrame.Navigate(typeof(SettingsPage), _host);
                break;
        }
    }
}
