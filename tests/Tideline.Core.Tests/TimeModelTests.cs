using System;
using System.IO;
using Tideline.Core.Data;
using Tideline.Core.Time;
using Xunit;

namespace Tideline.Core.Tests;

public sealed class TimeModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public TimeModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tideline-tm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "notes.db");
    }

    [Fact]
    public void Snooze_increments_count_and_pushes_remind()
    {
        using NotesDb db = NotesDb.Open(_dbPath);
        FixedClock clock = new(new DateTimeOffset(2026, 5, 23, 9, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds());
        NoteRepository repo = new(db, clock);
        var note = repo.Create("call the bank");
        long until = SnoozeOptions.PlusOneHour(clock);
        repo.Snooze(note.Id, until);
        var reloaded = repo.GetById(note.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(until, reloaded!.RemindAt);
        Assert.Equal(1, reloaded.SnoozeCount);

        repo.Snooze(note.Id, until + 3600_000);
        reloaded = repo.GetById(note.Id);
        Assert.Equal(2, reloaded!.SnoozeCount);
    }

    [Fact]
    public void Set_due_and_remind_round_trip_and_clear()
    {
        using NotesDb db = NotesDb.Open(_dbPath);
        FixedClock clock = new(0);
        NoteRepository repo = new(db, clock);
        var note = repo.Create("task");
        repo.SetDueAt(note.Id, 1_700_000_000_000);
        repo.SetRemindAt(note.Id, 1_700_000_500_000);
        repo.SetRecurrence(note.Id, "FREQ=DAILY");

        var reloaded = repo.GetById(note.Id)!;
        Assert.Equal(1_700_000_000_000, reloaded.DueAt);
        Assert.Equal(1_700_000_500_000, reloaded.RemindAt);
        Assert.Equal("FREQ=DAILY", reloaded.Recurrence);

        repo.SetDueAt(note.Id, null);
        repo.SetRemindAt(note.Id, null);
        repo.SetRecurrence(note.Id, null);
        reloaded = repo.GetById(note.Id)!;
        Assert.Null(reloaded.DueAt);
        Assert.Null(reloaded.RemindAt);
        Assert.Null(reloaded.Recurrence);
    }

    [Fact]
    public void Snooze_tomorrow_lands_at_local_morning()
    {
        FixedClock clock = new(new DateTimeOffset(2026, 5, 23, 23, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds());
        long ms = SnoozeOptions.Tomorrow(clock);
        DateTimeOffset local = DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime();
        Assert.Equal(SnoozeOptions.MorningHour, local.Hour);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); }
        catch { /* best effort */ }
    }
}
