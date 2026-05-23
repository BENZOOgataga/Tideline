using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tideline.App.Services;
using Tideline.App.ViewModels;
using Tideline.Core.Data;
using Tideline.Core.Models;
using Tideline.Core.Time;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Tideline.App.Views;

public sealed partial class NoteEditDialog : ContentDialog
{
    public NoteCard Card { get; }
    public Note Note => Card.Note;

    public string EditedBody => BodyBox.Text;
    public long? EditedRemindAt { get; private set; }
    public long? EditedDueAt { get; private set; }
    public string? EditedRecurrence => string.IsNullOrWhiteSpace(RecurrenceBox.Text) ? null : RecurrenceBox.Text.Trim();
    public bool EditedPinned => PinnedToggle.IsOn;
    public long? SnoozeUntilMs { get; private set; }
    public string? EditedSpaceId => (SpaceCombo.SelectedItem as SpaceOption)?.Id;
    public IReadOnlyList<string> EditedTagNames =>
        (TagsBox.Text ?? string.Empty)
            .Split(new[] { ' ', ',', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(TagRepository.Normalize)
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();

    private readonly IClock _clock;
    private readonly AppHost? _host;

    public NoteEditDialog(NoteCard card, IClock clock)
        : this(card, clock, hostForLookups: null)
    {
    }

    public NoteEditDialog(NoteCard card, IClock clock, AppHost? hostForLookups)
    {
        Card = card;
        _clock = clock;
        _host = hostForLookups;
        InitializeComponent();
        BodyBox.Text = card.Body;
        FramingText.Text = card.Framing;

        if (Note.RemindAt is long r)
        {
            RemindEnabled.IsChecked = true;
            DateTimeOffset local = DateTimeOffset.FromUnixTimeMilliseconds(r).ToLocalTime();
            RemindDate.Date = local;
            RemindTime.Time = local.TimeOfDay;
        }
        else
        {
            RemindDate.Date = _clock.Now().ToLocalTime();
            RemindTime.Time = new TimeSpan(9, 0, 0);
            ToggleRemindControls(false);
        }

        if (Note.DueAt is long d)
        {
            DueEnabled.IsChecked = true;
            DateTimeOffset local = DateTimeOffset.FromUnixTimeMilliseconds(d).ToLocalTime();
            DueDate.Date = local;
            DueTime.Time = local.TimeOfDay;
        }
        else
        {
            DueDate.Date = _clock.Now().ToLocalTime();
            DueTime.Time = new TimeSpan(17, 0, 0);
            ToggleDueControls(false);
        }

        RecurrenceBox.Text = Note.Recurrence ?? string.Empty;
        PinnedToggle.IsOn = Note.Pinned;

        PopulateSpacesAndTags(_host);
        RefreshAttachments();
        Closing += OnClosing;
    }

    private void PopulateSpacesAndTags(AppHost? host)
    {
        SpaceOption inbox = new(null, "Inbox");
        SpaceCombo.Items.Add(inbox);
        SpaceOption? selected = inbox;
        if (host is not null)
        {
            foreach (Space space in host.Spaces.All())
            {
                SpaceOption opt = new(space.Id, space.Name);
                SpaceCombo.Items.Add(opt);
                if (space.Id == Note.SpaceId) selected = opt;
            }
            var tagNames = host.Tags.ForNote(Note.Id).Select(t => "#" + t.Name);
            TagsBox.Text = string.Join(' ', tagNames);
        }
        SpaceCombo.SelectedItem = selected;
    }

    private void RefreshAttachments()
    {
        AttachmentsList.Items.Clear();
        if (_host is null)
        {
            AttachmentsHint.Text = "Attachments require the running app context.";
            return;
        }
        var atts = _host.Attachments.ForNote(Note.Id);
        foreach (Attachment a in atts)
        {
            AttachmentsList.Items.Add(BuildAttachmentRow(a));
        }
        AttachmentsHint.Text = atts.Count == 0
            ? "No attachments yet. Add files, folders, or URLs as references."
            : "Attachments are stored as references (paths or URLs), not copies.";
    }

    private FrameworkElement BuildAttachmentRow(Attachment a)
    {
        Grid row = new() { ColumnSpacing = 8, Padding = new Thickness(0, 4, 0, 4) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock kind = new() { Text = a.Kind, Opacity = 0.6 };
        Grid.SetColumn(kind, 0);
        row.Children.Add(kind);

        TextBlock path = new() { Text = a.DisplayName ?? a.PathOrUrl, TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis };
        ToolTipService.SetToolTip(path, a.PathOrUrl);
        Grid.SetColumn(path, 1);
        row.Children.Add(path);

        Button remove = new() { Content = "Remove" };
        remove.Click += (_, _) =>
        {
            _host!.Attachments.Remove(a.Id);
            RefreshAttachments();
        };
        Grid.SetColumn(remove, 2);
        row.Children.Add(remove);
        return row;
    }

    private async void AddFile_Click(object sender, RoutedEventArgs e)
    {
        if (_host is null) return;
        FileOpenPicker picker = new();
        picker.FileTypeFilter.Add("*");
        InitializePicker(picker);
        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null) return;
        _host.Attachments.Add(Note.Id, AttachmentKinds.FileRef, file.Path, file.Name);
        RefreshAttachments();
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_host is null) return;
        FolderPicker picker = new();
        picker.FileTypeFilter.Add("*");
        InitializePicker(picker);
        StorageFolder? folder = await picker.PickSingleFolderAsync();
        if (folder is null) return;
        _host.Attachments.Add(Note.Id, AttachmentKinds.FolderRef, folder.Path, folder.Name);
        RefreshAttachments();
    }

    private async void AddUrl_Click(object sender, RoutedEventArgs e)
    {
        if (_host is null) return;
        TextBox box = new() { PlaceholderText = "https://...", MinWidth = 360 };
        ContentDialog dlg = new()
        {
            Title = "Add URL",
            Content = box,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
        };
        ContentDialogResult result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(box.Text))
        {
            _host.Attachments.Add(Note.Id, AttachmentKinds.Url, box.Text.Trim());
            RefreshAttachments();
        }
    }

    private void InitializePicker(object picker)
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(App.Current?.GetActiveAppWindow() ?? throw new InvalidOperationException("No active window"));
        InitializeWithWindow.Initialize(picker, hwnd);
    }

    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (args.Result != ContentDialogResult.Primary) return;
        EditedRemindAt = RemindEnabled.IsChecked == true ? CombineDateTime(RemindDate.Date, RemindTime.Time) : null;
        EditedDueAt = DueEnabled.IsChecked == true ? CombineDateTime(DueDate.Date, DueTime.Time) : null;
    }

    private static long CombineDateTime(DateTimeOffset date, TimeSpan time)
    {
        DateTimeOffset combined = new(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds, date.Offset);
        return combined.ToUniversalTime().ToUnixTimeMilliseconds();
    }

    private void RemindEnabled_Toggled(object sender, RoutedEventArgs e) => ToggleRemindControls(RemindEnabled.IsChecked == true);
    private void DueEnabled_Toggled(object sender, RoutedEventArgs e) => ToggleDueControls(DueEnabled.IsChecked == true);

    private void ToggleRemindControls(bool enabled)
    {
        RemindDate.IsEnabled = enabled;
        RemindTime.IsEnabled = enabled;
    }

    private void ToggleDueControls(bool enabled)
    {
        DueDate.IsEnabled = enabled;
        DueTime.IsEnabled = enabled;
    }

    private void SnoozeOneHour_Click(object sender, RoutedEventArgs e) => ApplySnooze(SnoozeOptions.PlusOneHour(_clock), "+1 hour");
    private void SnoozeTonight_Click(object sender, RoutedEventArgs e) => ApplySnooze(SnoozeOptions.Tonight(_clock), "tonight");
    private void SnoozeTomorrow_Click(object sender, RoutedEventArgs e) => ApplySnooze(SnoozeOptions.Tomorrow(_clock), "tomorrow");
    private void SnoozeNextWeek_Click(object sender, RoutedEventArgs e) => ApplySnooze(SnoozeOptions.NextWeek(_clock), "next week");

    private void ApplySnooze(long untilMs, string label)
    {
        SnoozeUntilMs = untilMs;
        DateTimeOffset target = DateTimeOffset.FromUnixTimeMilliseconds(untilMs).ToLocalTime();
        SnoozeStatus.Text = $"Will snooze to {label} ({target:MMM d, HH:mm}) on Save.";
        RemindEnabled.IsChecked = true;
        ToggleRemindControls(true);
        RemindDate.Date = target;
        RemindTime.Time = target.TimeOfDay;
    }

    public sealed record SpaceOption(string? Id, string Name)
    {
        public override string ToString() => Name;
    }
}
