using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tideline.Core.Models;
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
    public string DueText => Note.DueAt is long ms ? RelativeTime.ReminderOrDue(ms, "Due", _clock) : string.Empty;

    public bool HasRemind => Note.RemindAt.HasValue;
    public string RemindText => Note.RemindAt is long ms ? RelativeTime.ReminderOrDue(ms, "Reminder", _clock) : string.Empty;

    public bool Pinned => Note.Pinned;

    public void Refresh()
    {
        Raise(nameof(Body));
        Raise(nameof(Framing));
        Raise(nameof(HasDue));
        Raise(nameof(DueText));
        Raise(nameof(HasRemind));
        Raise(nameof(RemindText));
        Raise(nameof(Pinned));
    }

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
