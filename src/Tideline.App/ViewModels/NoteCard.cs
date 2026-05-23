using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Tideline.Core.Models;
using Tideline.Core.Parsing;
using Tideline.Core.Time;

namespace Tideline.App.ViewModels;

public sealed class NoteCard : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id => Note.Id;
    public Note Note { get; }
    private readonly IClock _clock;

    public NoteCard(Note note, IClock clock)
    {
        Note = note;
        _clock = clock;
    }

    public string Body => Note.Body;

    public string Framing => RelativeTime.Written(Note.CreatedAt, _clock);

    public bool HasDue => Note.DueAt.HasValue;
    public Visibility DueVisibility => HasDue ? Visibility.Visible : Visibility.Collapsed;
    public string DueText => Note.DueAt is long ms ? RelativeTime.ReminderOrDue(ms, "Due", _clock) : string.Empty;

    public bool HasRemind => Note.RemindAt.HasValue;
    public Visibility RemindVisibility => HasRemind ? Visibility.Visible : Visibility.Collapsed;
    public string RemindText => Note.RemindAt is long ms ? RelativeTime.ReminderOrDue(ms, "Reminder", _clock) : string.Empty;

    public bool Pinned => Note.Pinned;
    public Visibility PinnedVisibility => Pinned ? Visibility.Visible : Visibility.Collapsed;

    public bool HasChecklist
    {
        get
        {
            var (_, total) = ChecklistParser.Progress(Note.Body);
            return total > 0;
        }
    }

    public Visibility ChecklistVisibility => HasChecklist ? Visibility.Visible : Visibility.Collapsed;

    public string ChecklistText
    {
        get
        {
            var (done, total) = ChecklistParser.Progress(Note.Body);
            return total == 0 ? string.Empty : $"{done} / {total} done";
        }
    }

    public void Refresh()
    {
        Raise(nameof(Body));
        Raise(nameof(Framing));
        Raise(nameof(HasDue));
        Raise(nameof(DueText));
        Raise(nameof(HasRemind));
        Raise(nameof(RemindText));
        Raise(nameof(Pinned));
        Raise(nameof(HasChecklist));
        Raise(nameof(ChecklistText));
    }

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
