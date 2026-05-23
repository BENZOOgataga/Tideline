using System;
using System.IO;
using System.Linq;
using Tideline.Core.Briefing;
using Tideline.Core.Data;
using Tideline.Core.Time;
using Xunit;

namespace Tideline.Core.Tests;

public sealed class BriefingTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public BriefingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tideline-brf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "notes.db");
    }

    private static long Ms(DateTimeOffset dto) => dto.ToUnixTimeMilliseconds();
    // Local DateTimeOffsets so BriefingService's local-day bucketing is
    // stable across developer time zones.
    private static long LocalMs(int y, int mo, int d, int h, int mi = 0)
        => new DateTimeOffset(new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Local)).ToUnixTimeMilliseconds();

    [Fact]
    public void Overdue_outranks_today_outranks_nudges()
    {
        FixedClock clock = new(LocalMs(2026, 5, 23, 10));
        using NotesDb db = NotesDb.Open(_dbPath);
        NoteRepository repo = new(db, clock);

        var overdue = repo.Create("overdue");
        repo.SetDueAt(overdue.Id, LocalMs(2026, 5, 20, 9));

        var today = repo.Create("today");
        repo.SetDueAt(today.Id, LocalMs(2026, 5, 23, 18));

        var nudge = repo.Create("nudge");
        repo.SetRemindAt(nudge.Id, LocalMs(2026, 5, 23, 9));

        BriefingService briefing = new(db, repo, clock);
        var ids = briefing.Compute().Items.Select(s => s.Note.Body).ToArray();
        Assert.Equal(new[] { "overdue", "today", "nudge" }, ids);
    }

    [Fact]
    public void Pinned_floats_to_top()
    {
        FixedClock clock = new(LocalMs(2026, 5, 23, 10));
        using NotesDb db = NotesDb.Open(_dbPath);
        NoteRepository repo = new(db, clock);

        var overdue = repo.Create("overdue heavy");
        repo.SetDueAt(overdue.Id, LocalMs(2026, 5, 10, 9));

        var pin = repo.Create("pinned reminder");
        repo.SetPinned(pin.Id, true);

        BriefingService briefing = new(db, repo, clock);
        var first = briefing.Compute().Items.First();
        Assert.Equal("pinned reminder", first.Note.Body);
        Assert.Equal(BriefingBucket.Pinned, first.Bucket);
    }

    [Fact]
    public void Aged_someday_only_for_unfiled_undated_older_than_threshold()
    {
        FixedClock clock = new(LocalMs(2026, 5, 23, 10));
        using NotesDb db = NotesDb.Open(_dbPath);
        NoteRepository repo = new(db, clock);

        FixedClock past = new(LocalMs(2026, 4, 1, 10));
        NoteRepository pastRepo = new(db, past);
        pastRepo.Create("old idea");

        repo.Create("brand new");

        BriefingService briefing = new(db, repo, clock);
        var aged = briefing.Compute().InBucket(BriefingBucket.AgedSomeday);
        Assert.Single(aged);
        Assert.Equal("old idea", aged[0].Note.Body);
    }

    [Fact]
    public void Empty_when_nothing_qualifies()
    {
        FixedClock clock = new(LocalMs(2026, 5, 23, 10));
        using NotesDb db = NotesDb.Open(_dbPath);
        NoteRepository repo = new(db, clock);
        repo.Create("plain thought");

        BriefingService briefing = new(db, repo, clock);
        Assert.True(briefing.Compute().IsEmpty);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); }
        catch { }
    }
}
