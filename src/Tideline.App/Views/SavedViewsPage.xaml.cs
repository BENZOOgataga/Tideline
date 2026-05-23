using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;
using Tideline.Core.Models;

namespace Tideline.App.Views;

public sealed partial class SavedViewsPage : Page
{
    private AppHost? _host;

    public SavedViewsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _host = e.Parameter as AppHost;
        Reload();
        base.OnNavigatedTo(e);
    }

    private void Reload()
    {
        ViewsList.Items.Clear();
        if (_host is null) return;
        var all = _host.SavedFilters.All();
        if (all.Count == 0)
        {
            ViewsList.Items.Add(new TextBlock { Text = "No saved views yet. Save one from The List.", Opacity = 0.6 });
            return;
        }
        foreach (SavedFilter f in all)
        {
            ViewsList.Items.Add(BuildRow(f));
        }
    }

    private FrameworkElement BuildRow(SavedFilter f)
    {
        Grid row = new() { ColumnSpacing = 12, Padding = new Thickness(0, 8, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock name = new()
        {
            Text = f.Name,
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(name, 0);
        row.Children.Add(name);

        TextBlock query = new()
        {
            Text = f.Query,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis,
        };
        Grid.SetColumn(query, 1);
        row.Children.Add(query);

        Button open = new() { Content = "Open" };
        open.Click += (_, _) =>
        {
            if (_host is null) return;
            this.Frame.Navigate(typeof(ListPage), new ListPage.NavArg(_host, null, false, f.Query));
        };
        Grid.SetColumn(open, 2);
        row.Children.Add(open);

        Button delete = new() { Content = "Delete" };
        delete.Click += (_, _) =>
        {
            if (_host is null) return;
            _host.SavedFilters.Delete(f.Id);
            Reload();
        };
        Grid.SetColumn(delete, 3);
        row.Children.Add(delete);

        return row;
    }
}
