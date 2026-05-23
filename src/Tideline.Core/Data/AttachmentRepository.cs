using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Tideline.Core.Models;
using Tideline.Core.Time;

namespace Tideline.Core.Data;

public sealed class AttachmentRepository
{
    private readonly NotesDb _db;
    private readonly IClock _clock;

    public AttachmentRepository(NotesDb db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public Attachment Add(string noteId, string kind, string pathOrUrl, string? displayName = null)
    {
        Attachment att = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            NoteId = noteId,
            Kind = kind,
            PathOrUrl = pathOrUrl,
            DisplayName = displayName,
            AddedAt = _clock.NowMs(),
        };
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "INSERT INTO attachments(id, note_id, kind, path_or_url, display_name, added_at) VALUES ($id, $note, $kind, $p, $d, $t)";
        cmd.Parameters.AddWithValue("$id", att.Id);
        cmd.Parameters.AddWithValue("$note", att.NoteId);
        cmd.Parameters.AddWithValue("$kind", att.Kind);
        cmd.Parameters.AddWithValue("$p", att.PathOrUrl);
        cmd.Parameters.AddWithValue("$d", (object?)att.DisplayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$t", att.AddedAt);
        cmd.ExecuteNonQuery();
        return att;
    }

    public IReadOnlyList<Attachment> ForNote(string noteId)
    {
        List<Attachment> list = new();
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, note_id, kind, path_or_url, display_name, added_at FROM attachments WHERE note_id = $id ORDER BY added_at";
        cmd.Parameters.AddWithValue("$id", noteId);
        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Attachment
            {
                Id = r.GetString(0),
                NoteId = r.GetString(1),
                Kind = r.GetString(2),
                PathOrUrl = r.GetString(3),
                DisplayName = r.IsDBNull(4) ? null : r.GetString(4),
                AddedAt = r.GetInt64(5),
            });
        }
        return list;
    }

    public void Remove(string id)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM attachments WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}
