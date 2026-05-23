using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;
using Tideline.App.ViewModels;
using Tideline.Core.Filtering;
using Tideline.Core.Models;

namespace Tideline.App.Views;

public sealed partial class ListPage : Page
{
    public record NavArg(AppHost? Host, string? SpaceId, bool IncludeUnfiledOnly, string? PresetQuery = null);

    private AppHost? _host;
    private NavArg? _arg;

    public ObservableCollection<NoteCard> Items { get; } = new();

    public ListPage()
    {
        InitializeComponent();
        NotesList.ItemsSource = Items;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is NavArg arg)
        {
            _arg = arg;
            _host = arg.Host;
            if (!string.IsNullOrEmpty(arg.PresetQuery))
            {
                SearchBox.Text = arg.PresetQuery;
            }
        }
        else
        {
            _host = e.Parameter as AppHost;
            _arg = new NavArg(_host, null, false);
        }
        Reload();
        base.OnNavigatedTo(e);
    }

    private void Reload(string? query = null)
    {
        Items.Clear();
        if (_host is null) return;
        string q = query ?? SearchBox.Text ?? string.Empty;
        IReadOnlyList<Note> notes;
        if (!string.IsNullOrWhiteSpace(q))
        {
            FilterQuery parsed = FilterParser.Parse(q);
            notes = _host.Notes.Query(parsed);
        }
        else if (_arg?.IncludeUnfiledOnly == true)
        {
            notes = _host.Notes.InSpace(null);
        }
        else if (_arg?.SpaceId is string sid)
        {
            notes = _host.Notes.InSpace(sid);
        }
        else
        {
            notes = _host.Notes.All();
        }
        foreach (Note note in notes)
        {
            Items.Add(new NoteCard(note, _host.Clock));
        }
        int count = Items.Count;
        SubtitleText.Text = count == 0
            ? "No notes match. Try a different query or capture a thought with Ctrl+Alt+N."
            : $"{count} note{(count == 1 ? string.Empty : "s")}.";
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            Reload(sender.Text);
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        Reload(sender.Text);
    }

    private async void NotesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not NoteCard card || _host is null) return;
        NoteEditDialog dialog = new(card, _host.Clock, _host)
        {
            XamlRoot = this.XamlRoot,
        };
        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _host.Notes.UpdateBody(card.Id, dialog.EditedBody);
            if (dialog.SnoozeUntilMs is long snoozeMs)
            {
                _host.Notes.Snooze(card.Id, snoozeMs);
            }
            else
            {
                _host.Notes.SetRemindAt(card.Id, dialog.EditedRemindAt);
            }
            _host.Notes.SetDueAt(card.Id, dialog.EditedDueAt);
            _host.Notes.SetRecurrence(card.Id, dialog.EditedRecurrence);
            _host.Notes.SetPinned(card.Id, dialog.EditedPinned);
            _host.Notes.SetSpace(card.Id, dialog.EditedSpaceId);
            _host.Tags.ReplaceForNote(card.Id, dialog.EditedTagNames);
        }
        else if (result == ContentDialogResult.Secondary)
        {
            _host.Notes.Archive(card.Id);
        }
        Reload();
    }

    private void ArchiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is NoteCard card && _host is not null)
        {
            _host.Notes.Archive(card.Id);
            Reload();
        }
    }

    private async void SaveViewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_host is null) return;
        string query = SearchBox.Text?.Trim() ?? string.Empty;
        if (query.Length == 0) return;
        TextBox nameBox = new() { PlaceholderText = "View name", MinWidth = 300 };
        ContentDialog dlg = new()
        {
            Title = "Save view",
            Content = nameBox,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
        };
        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            _host.SavedFilters.Create(nameBox.Text.Trim(), query);
        }
    }
}
