using System;
using Tideline.Core.Data;
using Tideline.Core.Time;

namespace Tideline.App.Services;

/// <summary>
/// Lightweight composition root. Owns the database connection lifetime.
/// </summary>
public sealed class AppHost : IDisposable
{
    public IClock Clock { get; }
    public NotesDb Database { get; }
    public NoteRepository Notes { get; }
    public string? HotkeyError { get; set; }

    private bool _disposed;

    private AppHost(IClock clock, NotesDb db)
    {
        Clock = clock;
        Database = db;
        Notes = new NoteRepository(db, clock);
    }

    public static AppHost Boot()
    {
        AppPaths.EnsureDirectories();
        NotesDb db = NotesDb.OpenDefault();
        return new AppHost(new SystemClock(), db);
    }

    public int RunStartupPurge()
    {
        ArchivePurge purge = new(Database, Clock);
        return purge.RunOnce();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Database.Dispose();
    }
}
