using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Tideline.Core.Models;
using Tideline.Core.Time;

namespace Tideline.Core.Data;

public sealed class NoteRepository
{
    private readonly NotesDb _db;
    private readonly IClock _clock;

    public NoteRepository(NotesDb db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public Note Create(string body, string? spaceId = null)
    {
        Note note = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Body = body ?? string.Empty,
            CreatedAt = _clock.NowMs(),
            UpdatedAt = _clock.NowMs(),
            SpaceId = spaceId,
        };

        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = @"
INSERT INTO notes(id, body, created_at, updated_at, remind_at, due_at, recurrence, archived, archived_at, space_id, snooze_count, pinned)
VALUES ($id, $body, $createdAt, $updatedAt, NULL, NULL, NULL, 0, NULL, $spaceId, 0, 0)";
        cmd.Parameters.AddWithValue("$id", note.Id);
        cmd.Parameters.AddWithValue("$body", note.Body);
        cmd.Parameters.AddWithValue("$createdAt", note.CreatedAt);
        cmd.Parameters.AddWithValue("$updatedAt", note.UpdatedAt);
        cmd.Parameters.AddWithValue("$spaceId", (object?)note.SpaceId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        return note;
    }

    public Note? GetById(string id)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, body, created_at, updated_at, remind_at, due_at, recurrence, archived, archived_at, space_id, snooze_count, pinned FROM notes WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using SqliteDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public IReadOnlyList<Note> All(bool includeArchived = false)
    {
        List<Note> list = new();
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = includeArchived
            ? "SELECT id, body, created_at, updated_at, remind_at, due_at, recurrence, archived, archived_at, space_id, snooze_count, pinned FROM notes ORDER BY created_at DESC"
            : "SELECT id, body, created_at, updated_at, remind_at, due_at, recurrence, archived, archived_at, space_id, snooze_count, pinned FROM notes WHERE archived = 0 ORDER BY created_at DESC";
        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(Map(reader));
        }
        return list;
    }

    public int Count(bool includeArchived = false)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = includeArchived
            ? "SELECT COUNT(*) FROM notes"
            : "SELECT COUNT(*) FROM notes WHERE archived = 0";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void UpdateBody(string id, string body)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE notes SET body = $body, updated_at = $u WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$body", body ?? string.Empty);
        cmd.Parameters.AddWithValue("$u", _clock.NowMs());
        cmd.ExecuteNonQuery();
    }

    public void Archive(string id)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE notes SET archived = 1, archived_at = $now, updated_at = $now WHERE id = $id AND archived = 0";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$now", _clock.NowMs());
        cmd.ExecuteNonQuery();
    }

    public void Restore(string id)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE notes SET archived = 0, archived_at = NULL, updated_at = $now WHERE id = $id AND archived = 1";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$now", _clock.NowMs());
        cmd.ExecuteNonQuery();
    }

    public void SetPinned(string id, bool pinned)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE notes SET pinned = $p, updated_at = $u WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$p", pinned ? 1 : 0);
        cmd.Parameters.AddWithValue("$u", _clock.NowMs());
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<Note> Search(string query, int limit = 200)
    {
        List<Note> list = new();
        if (string.IsNullOrWhiteSpace(query)) return list;
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = @"
SELECT n.id, n.body, n.created_at, n.updated_at, n.remind_at, n.due_at, n.recurrence, n.archived, n.archived_at, n.space_id, n.snooze_count, n.pinned
FROM notes n
JOIN notes_fts f ON f.rowid = n.rowid
WHERE notes_fts MATCH $q
ORDER BY n.created_at DESC
LIMIT $limit";
        cmd.Parameters.AddWithValue("$q", BuildFtsQuery(query));
        cmd.Parameters.AddWithValue("$limit", limit);
        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(Map(reader));
        return list;
    }

    private static string BuildFtsQuery(string raw)
    {
        string trimmed = raw.Trim();
        if (trimmed.Length == 0) return string.Empty;
        // Defensive: quote each token, prefix-match the last one.
        string[] parts = trimmed.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string safe = parts[i].Replace("\"", "\"\"");
            parts[i] = i == parts.Length - 1 ? $"\"{safe}\"*" : $"\"{safe}\"";
        }
        return string.Join(" ", parts);
    }

    private static Note Map(SqliteDataReader r) => new()
    {
        Id = r.GetString(0),
        Body = r.GetString(1),
        CreatedAt = r.GetInt64(2),
        UpdatedAt = r.GetInt64(3),
        RemindAt = r.IsDBNull(4) ? null : r.GetInt64(4),
        DueAt = r.IsDBNull(5) ? null : r.GetInt64(5),
        Recurrence = r.IsDBNull(6) ? null : r.GetString(6),
        Archived = r.GetInt64(7) != 0,
        ArchivedAt = r.IsDBNull(8) ? null : r.GetInt64(8),
        SpaceId = r.IsDBNull(9) ? null : r.GetString(9),
        SnoozeCount = r.GetInt32(10),
        Pinned = r.GetInt64(11) != 0,
    };
}
