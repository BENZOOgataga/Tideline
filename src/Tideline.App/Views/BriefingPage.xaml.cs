using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;
using Tideline.App.ViewModels;
using Tideline.Core.Briefing;

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
        Reload();
        base.OnNavigatedTo(e);
    }

    private void Reload()
    {
        SectionsHost.Items.Clear();
        if (_host is null) return;

        BriefingResult result = _host.Briefing.Compute();
        if (result.IsEmpty)
        {
            EmptyState.Visibility = Visibility.Visible;
            ContentScroll.Visibility = Visibility.Collapsed;
            SubtitleText.Text = "Nothing needs attention right now.";
            return;
        }
        EmptyState.Visibility = Visibility.Collapsed;
        ContentScroll.Visibility = Visibility.Visible;
        SubtitleText.Text = $"{result.Items.Count} item{(result.Items.Count == 1 ? string.Empty : "s")} to look at.";

        AddSection("Pinned", result.InBucket(BriefingBucket.Pinned));
        AddSection("Overdue", result.InBucket(BriefingBucket.Overdue));
        AddSection("Due today", result.InBucket(BriefingBucket.DueToday));
        AddSection("Nudges", result.InBucket(BriefingBucket.Nudges));
        AddSection("Aged someday", result.InBucket(BriefingBucket.AgedSomeday));
    }

    private void AddSection(string header, IReadOnlyList<ScoredNote> items)
    {
        if (items.Count == 0 || _host is null) return;
        StackPanel section = new() { Spacing = 8, Margin = new Thickness(0, 0, 0, 18) };

        TextBlock title = new()
        {
            Text = header,
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
            Opacity = 0.7,
        };
        section.Children.Add(title);

        foreach (ScoredNote scored in items)
        {
            section.Children.Add(BuildCard(scored));
        }
        SectionsHost.Items.Add(section);
    }

    private Border BuildCard(ScoredNote scored)
    {
        NoteCard vm = new(scored.Note, _host!.Clock);
        Border card = new()
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
        };
        StackPanel sp = new() { Spacing = 6 };

        TextBlock body = new()
        {
            Text = vm.Body,
            TextWrapping = TextWrapping.Wrap,
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
        };
        sp.Children.Add(body);

        StackPanel meta = new() { Orientation = Orientation.Horizontal, Spacing = 12 };
        meta.Children.Add(MakeCaption(vm.Framing));
        if (vm.HasDue) meta.Children.Add(MakeCaption(vm.DueText));
        if (vm.HasRemind) meta.Children.Add(MakeCaption(vm.RemindText));
        if (scored.Note.SnoozeCount >= BriefingService.DefaultDecayThreshold)
        {
            meta.Children.Add(MakeCaption($"Snoozed {scored.Note.SnoozeCount} times, still relevant?"));
        }
        sp.Children.Add(meta);

        StackPanel actions = new() { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
        Button done = new() { Content = "Done" };
        done.Click += (_, _) => { _host!.Notes.Archive(scored.Note.Id); Reload(); };
        actions.Children.Add(done);
        Button snooze = new() { Content = "Snooze tomorrow" };
        snooze.Click += (_, _) =>
        {
            _host!.Notes.Snooze(scored.Note.Id, Tideline.Core.Time.SnoozeOptions.Tomorrow(_host.Clock));
            Reload();
        };
        actions.Children.Add(snooze);
        sp.Children.Add(actions);

        card.Child = sp;
        return card;
    }

    private static TextBlock MakeCaption(string text) => new()
    {
        Text = text,
        Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
        Opacity = 0.6,
    };
}
