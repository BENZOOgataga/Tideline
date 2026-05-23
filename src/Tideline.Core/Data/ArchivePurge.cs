using System;
using Microsoft.Data.Sqlite;
using Tideline.Core.Time;

namespace Tideline.Core.Data;

/// <summary>
/// Conservative purge of archived notes that crossed the retention threshold.
/// Never deletes unarchived notes. Never deletes pinned notes. Runs at launch only.
/// </summary>
public sealed class ArchivePurge
{
    public const int DefaultRetentionDays = 90;

    private readonly NotesDb _db;
    private readonly IClock _clock;
    private readonly int _retentionDays;

    public ArchivePurge(NotesDb db, IClock clock, int retentionDays = DefaultRetentionDays)
    {
        _db = db;
        _clock = clock;
        _retentionDays = retentionDays > 0 ? retentionDays : DefaultRetentionDays;
    }

    public int RunOnce()
    {
        long thresholdMs = _clock.NowMs() - (long)TimeSpan.FromDays(_retentionDays).TotalMilliseconds;
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = @"
DELETE FROM notes
WHERE archived = 1
  AND pinned = 0
  AND archived_at IS NOT NULL
  AND archived_at <= $threshold";
        cmd.Parameters.AddWithValue("$threshold", thresholdMs);
        return cmd.ExecuteNonQuery();
    }
}
