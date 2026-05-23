using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;

namespace Tideline.App.Views;

public sealed partial class ListPage : Page
{
    private AppHost? _host;

    public ListPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _host = e.Parameter as AppHost;
        if (_host is not null)
        {
            int count = _host.Notes.Count();
            StatusText.Text = count == 0 ? "No notes yet." : $"{count} note{(count == 1 ? string.Empty : "s")}.";
        }
        base.OnNavigatedTo(e);
    }
}
