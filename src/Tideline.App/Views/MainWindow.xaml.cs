using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        Title = "Tideline";

        AppWindow.SetIcon(System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "Tideline.ico"));
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
        AppWindow.Closing += OnAppWindowClosing;

        Navigate("briefing");
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // Close-to-tray: closing the window must not exit the process,
        // because the resident app owns the IPC listener and global hotkey.
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
