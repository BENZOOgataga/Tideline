using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tideline.App.ViewModels;
using Tideline.Core.Models;
using Tideline.Core.Time;

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

    private readonly IClock _clock;

    public NoteEditDialog(NoteCard card, IClock clock)
    {
        Card = card;
        _clock = clock;
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

        Closing += OnClosing;
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
}
