using System;
using System.IO;
using Tideline.Core.Data;
using Tideline.Core.Time;
using Xunit;

namespace Tideline.Core.Tests;

public sealed class NotesDbTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public NotesDbTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tideline-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "notes.db");
    }

    [Fact]
    public void Opens_and_creates_schema()
    {
        using NotesDb db = NotesDb.Open(_dbPath);
        Assert.True(File.Exists(_dbPath));
    }

    [Fact]
    public void Create_get_and_archive_round_trip()
    {
        using NotesDb db = NotesDb.Open(_dbPath);
        FixedClock clock = new(1_700_000_000_000);
        NoteRepository repo = new(db, clock);

        var created = repo.Create("Buy bread");
        Assert.NotNull(repo.GetById(created.Id));
        Assert.Equal(1, repo.Count());

        clock.Advance(TimeSpan.FromMinutes(5));
        repo.Archive(created.Id);
        Assert.Equal(0, repo.Count());
        Assert.Equal(1, repo.Count(includeArchived: true));

        clock.Advance(TimeSpan.FromMinutes(5));
        repo.Restore(created.Id);
        Assert.Equal(1, repo.Count());
    }

    [Fact]
    public void Full_text_search_finds_by_prefix()
    {
        using NotesDb db = NotesDb.Open(_dbPath);
        FixedClock clock = new(1_700_000_000_000);
        NoteRepository repo = new(db, clock);
        repo.Create("Refactor authentication middleware");
        repo.Create("Buy milk and eggs");

        var hits = repo.Search("auth");
        Assert.Single(hits);
        Assert.Contains("auth", hits[0].Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Archive_purge_only_deletes_past_threshold_unpinned()
    {
        using NotesDb db = NotesDb.Open(_dbPath);
        FixedClock clock = new(1_700_000_000_000);
        NoteRepository repo = new(db, clock);

        var old = repo.Create("ancient");
        var recent = repo.Create("recent");
        var pinned = repo.Create("pinned and old");
        repo.SetPinned(pinned.Id, true);

        repo.Archive(old.Id);
        repo.Archive(pinned.Id);
        clock.Advance(TimeSpan.FromDays(95));
        repo.Archive(recent.Id);

        ArchivePurge purge = new(db, clock, retentionDays: 90);
        int deleted = purge.RunOnce();
        Assert.Equal(1, deleted);
        Assert.Null(repo.GetById(old.Id));
        Assert.NotNull(repo.GetById(recent.Id));
        Assert.NotNull(repo.GetById(pinned.Id));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // best effort
        }
    }
}
