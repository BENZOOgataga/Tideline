using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;

namespace Tideline.App.Views;

public sealed partial class SettingsPage : Page
{
    private AppHost? _host;
    private readonly AutoStartService _autoStart = new();
    private bool _suppressToggle;

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
        _suppressToggle = true;
        AutoStartToggle.IsOn = _autoStart.IsEnabled();
        _suppressToggle = false;
        base.OnNavigatedTo(e);
    }

    private void AutoStartToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suppressToggle) return;
        AutoStartWarning.IsOpen = false;
        AutoStartWarning.Message = string.Empty;
        if (AutoStartToggle.IsOn)
        {
            var (ok, err) = _autoStart.Enable(_autoStart.ExePath);
            if (!ok)
            {
                _suppressToggle = true;
                AutoStartToggle.IsOn = false;
                _suppressToggle = false;
                AutoStartWarning.Title = "Could not enable auto-start";
                AutoStartWarning.Message = err ?? "schtasks failed.";
                AutoStartWarning.IsOpen = true;
            }
        }
        else
        {
            _autoStart.Disable();
        }
    }
}
