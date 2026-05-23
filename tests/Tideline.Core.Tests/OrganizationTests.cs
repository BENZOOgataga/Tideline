using System;
using System.IO;
using System.Linq;
using Tideline.Core.Data;
using Tideline.Core.Parsing;
using Tideline.Core.Time;
using Xunit;

namespace Tideline.Core.Tests;

public sealed class OrganizationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public OrganizationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tideline-org-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "notes.db");
    }

    [Fact]
    public void Space_create_and_archive_sweeps_notes()
    {
        using NotesDb db = NotesDb.Open(_dbPath);
        FixedClock clock = new(1_700_000_000_000);
        NoteRepository notes = new(db, clock);
        SpaceRepository spaces = new(db, clock);

        var work = spaces.Create("Work", "#0078D4");
        var n1 = notes.Create("write doc");
        var n2 = notes.Create("call partner");
        notes.SetSpace(n1.Id, work.Id);
        notes.SetSpace(n2.Id, work.Id);

        Assert.Equal(2, notes.InSpace(work.Id).Count);
        spaces.Archive(work.Id);

        Assert.Empty(notes.InSpace(work.Id));
        var inbox = notes.InSpace(null);
        Assert.Empty(inbox);
        Assert.Equal(2, notes.Count(includeArchived: true));
    }

    [Fact]
    public void Tag_get_or_create_normalizes_and_dedupes()
    {
        using NotesDb db = NotesDb.Open(_dbPath);
        TagRepository tags = new(db);
        var a = tags.GetOrCreate("#URGENT");
        var b = tags.GetOrCreate("urgent");
        Assert.Equal(a.Id, b.Id);
        Assert.Equal("urgent", a.Name);
    }

    [Fact]
    public void Tag_replace_for_note_updates_attachments()
    {
        using NotesDb db = NotesDb.Open(_dbPath);
        FixedClock clock = new(1_700_000_000_000);
        NoteRepository notes = new(db, clock);
        TagRepository tags = new(db);
        var note = notes.Create("body");
        tags.ReplaceForNote(note.Id, new[] { "work", "urgent" });
        Assert.Equal(2, tags.ForNote(note.Id).Count);
        tags.ReplaceForNote(note.Id, new[] { "personal" });
        var current = tags.ForNote(note.Id).Select(t => t.Name).ToArray();
        Assert.Single(current);
        Assert.Equal("personal", current[0]);
    }

    [Fact]
    public void Hashtag_parser_finds_and_dedupes_unicode_tokens()
    {
        var tags = HashtagParser.Extract("call #bank then write #Doc-1 and also #bank again #plan_b");
        Assert.Equal(new[] { "bank", "doc-1", "plan_b" }, tags);
    }

    [Fact]
    public void Hashtag_parser_ignores_inside_url()
    {
        var tags = HashtagParser.Extract("see https://example.com/page#fragment for context #real");
        Assert.Equal(new[] { "real" }, tags);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); }
        catch { }
    }
}
