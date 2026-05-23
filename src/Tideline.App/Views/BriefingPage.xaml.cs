using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;

namespace Tideline.App.Views;

public sealed partial class BriefingPage : Page
{
    private AppHost? _host;

    public BriefingPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _host = e.Parameter as AppHost;
        base.OnNavigatedTo(e);
    }
}
