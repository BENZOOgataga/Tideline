using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;
using Tideline.App.ViewModels;
using Tideline.Core.Models;

namespace Tideline.App.Views;

public sealed partial class ListPage : Page
{
    private AppHost? _host;

    public ObservableCollection<NoteCard> Items { get; } = new();

    public ListPage()
    {
        InitializeComponent();
        NotesList.ItemsSource = Items;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _host = e.Parameter as AppHost;
        Reload();
        base.OnNavigatedTo(e);
    }

    private void Reload(string? query = null)
    {
        Items.Clear();
        if (_host is null) return;
        IReadOnlyList<Note> notes = string.IsNullOrWhiteSpace(query)
            ? _host.Notes.All()
            : _host.Notes.Search(query);
        foreach (Note note in notes)
        {
            Items.Add(new NoteCard(note, _host.Clock));
        }
        int count = Items.Count;
        SubtitleText.Text = count == 0
            ? "No notes yet. Capture one with Ctrl+Alt+N."
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
        NoteEditDialog dialog = new(card, _host.Clock)
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
        }
        else if (result == ContentDialogResult.Secondary)
        {
            _host.Notes.Archive(card.Id);
        }
        Reload(SearchBox.Text);
    }

    private void ArchiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is NoteCard card && _host is not null)
        {
            _host.Notes.Archive(card.Id);
            Reload(SearchBox.Text);
        }
    }
}
