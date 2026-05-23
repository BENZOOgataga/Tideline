using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;

namespace Tideline.App.Views;

public sealed partial class SettingsPage : Page
{
    private AppHost? _host;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _host = e.Parameter as AppHost;
        if (_host is not null)
        {
            DbPathText.Text = _host.Database.DatabasePath;
            if (!string.IsNullOrWhiteSpace(_host.HotkeyError))
            {
                HotkeyWarning.Message = _host.HotkeyError;
                HotkeyWarning.IsOpen = true;
            }
        }
        base.OnNavigatedTo(e);
    }
}
