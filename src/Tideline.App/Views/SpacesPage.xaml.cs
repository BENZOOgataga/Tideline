using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;
using Tideline.Core.Models;
using Windows.UI;

namespace Tideline.App.Views;

public sealed partial class SpacesPage : Page
{
    private AppHost? _host;

    public SpacesPage()
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
        SpacesList.Items.Clear();
        if (_host is null) return;
        foreach (Space space in _host.Spaces.All())
        {
            SpacesList.Items.Add(BuildRow(space));
        }
        if (SpacesList.Items.Count == 0)
        {
            TextBlock empty = new()
            {
                Text = "No spaces yet. Add one above.",
                Opacity = 0.6,
            };
            SpacesList.Items.Add(empty);
        }
    }

    private FrameworkElement BuildRow(Space space)
    {
        Grid row = new() { ColumnSpacing = 12, Padding = new Thickness(0, 8, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Border swatch = new()
        {
            Width = 10,
            Height = 24,
            CornerRadius = new CornerRadius(3),
            Background = ColorBrushFor(space.Color),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(swatch, 0);
        row.Children.Add(swatch);

        TextBlock name = new()
        {
            Text = space.Name,
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(name, 1);
        row.Children.Add(name);

        TextBlock id = new()
        {
            Text = $"id: {space.Id}",
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Opacity = 0.5,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(id, 2);
        row.Children.Add(id);

        Button archive = new() { Content = "Archive" };
        archive.Click += (_, _) =>
        {
            if (_host is null) return;
            _host.Spaces.Archive(space.Id);
            Reload();
        };
        Grid.SetColumn(archive, 3);
        row.Children.Add(archive);

        return row;
    }

    private static SolidColorBrush ColorBrushFor(string? hex)
    {
        if (!string.IsNullOrWhiteSpace(hex) && TryParseHex(hex, out Color c))
        {
            return new SolidColorBrush(c);
        }
        return (SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"];
    }

    private static bool TryParseHex(string hex, out Color color)
    {
        color = default;
        string s = hex.Trim().TrimStart('#');
        if (s.Length == 6 && uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out uint rgb))
        {
            color = Color.FromArgb(0xFF, (byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));
            return true;
        }
        if (s.Length == 8 && uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out uint argb))
        {
            color = Color.FromArgb((byte)((argb >> 24) & 0xFF), (byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF));
            return true;
        }
        return false;
    }

    private void AddSpaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_host is null) return;
        string name = NewSpaceName.Text?.Trim() ?? string.Empty;
        if (name.Length == 0) return;
        string color = NewSpaceColor.Text?.Trim() ?? string.Empty;
        _host.Spaces.Create(name, string.IsNullOrEmpty(color) ? null : color);
        NewSpaceName.Text = string.Empty;
        NewSpaceColor.Text = string.Empty;
        Reload();
    }
}
