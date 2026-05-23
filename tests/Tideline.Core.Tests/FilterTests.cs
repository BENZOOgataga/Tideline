using System;
using System.IO;
using System.Linq;
using Tideline.Core.Data;
using Tideline.Core.Filtering;
using Tideline.Core.Time;
using Xunit;

namespace Tideline.Core.Tests;

public sealed class FilterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public FilterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tideline-flt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "notes.db");
    }

    [Fact]
    public void Parser_extracts_tags_due_space_and_text()
    {
        var q = FilterParser.Parse("#work #urgent due:thisweek space:learning rebuild auth");
        Assert.Equal(new[] { "work", "urgent" }, q.Tags);
        Assert.Equal(TagMatchMode.All, q.TagMode);
        Assert.Equal("learning", q.SpaceName);
        Assert.Equal(DueRange.ThisWeek, q.Due);
        Assert.Equal("rebuild auth", q.Text);
    }

    [Fact]
    public void Parser_supports_any_prefix()
    {
        var q = FilterParser.Parse("any:#idea,#someday other text");
        Assert.Equal(TagMatchMode.Any, q.TagMode);
        Assert.Equal(new[] { "idea", "someday" }, q.Tags);
        Assert.Equal("other text", q.Text);
    }

    [Fact]
    public void Query_all_mode_requires_every_tag()
    {
        using NotesDb db = NotesDb.Open(_dbPath);
        FixedClock clock = new(1_700_000_000_000);
        NoteRepository notes = new(db, clock);
        TagRepository tags = new(db);

        var a = notes.Create("first");
        var b = notes.Create("second");
        tags.ReplaceForNote(a.Id, new[] { "work", "urgent" });
        tags.ReplaceForNote(b.Id, new[] { "work" });

        var hits = notes.Query(FilterParser.Parse("#work #urgent"));
        Assert.Single(hits);
        Assert.Equal(a.Id, hits[0].Id);

        hits = notes.Query(FilterParser.Parse("any:#work,#urgent"));
        Assert.Equal(2, hits.Count);
    }

    [Fact]
    public void Query_due_today_includes_only_today_dated()
    {
        FixedClock clock = new(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds());
        using NotesDb db = NotesDb.Open(_dbPath);
        NoteRepository notes = new(db, clock);
        var today = notes.Create("today");
        var tomorrow = notes.Create("tomorrow");
        notes.SetDueAt(today.Id, new DateTimeOffset(2026, 5, 23, 18, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds());
        notes.SetDueAt(tomorrow.Id, new DateTimeOffset(2026, 5, 24, 18, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds());

        var hits = notes.Query(FilterParser.Parse("due:today")).Select(n => n.Body).ToArray();
        Assert.Equal(new[] { "today" }, hits);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); }
        catch { }
    }
}
