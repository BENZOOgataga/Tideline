using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;
using Tideline.App.ViewModels;
using Tideline.Core.Models;

namespace Tideline.App.Views;

public sealed partial class StreamPage : Page
{
    private AppHost? _host;
    public ObservableCollection<NoteCard> Items { get; } = new();

    public StreamPage()
    {
        InitializeComponent();
        StreamItems.ItemsSource = Items;
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
        IReadOnlyList<Note> notes;
        if (string.IsNullOrWhiteSpace(query))
        {
            notes = _host.Notes.Stream();
        }
        else
        {
            // Search returns newest first, reverse to keep stream order.
            var hits = _host.Notes.Search(query);
            var list = new List<Note>(hits);
            list.Reverse();
            notes = list;
        }
        foreach (Note note in notes)
        {
            Items.Add(new NoteCard(note, _host.Clock));
        }
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
}
