using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Tideline.Core.Models;

namespace Tideline.Core.Data;

public sealed class TagRepository
{
    private readonly NotesDb _db;

    public TagRepository(NotesDb db)
    {
        _db = db;
    }

    public static string Normalize(string name)
        => name.Trim().TrimStart('#').ToLowerInvariant();

    public Tag GetOrCreate(string name)
    {
        string normalized = Normalize(name);
        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException("Tag name cannot be empty.", nameof(name));
        }
        using SqliteCommand select = _db.Connection.CreateCommand();
        select.CommandText = "SELECT id, name FROM tags WHERE name = $n";
        select.Parameters.AddWithValue("$n", normalized);
        using (SqliteDataReader r = select.ExecuteReader())
        {
            if (r.Read())
            {
                return new Tag { Id = r.GetString(0), Name = r.GetString(1) };
            }
        }
        Tag tag = new() { Id = Guid.NewGuid().ToString("N"), Name = normalized };
        using SqliteCommand insert = _db.Connection.CreateCommand();
        insert.CommandText = "INSERT INTO tags(id, name) VALUES ($id, $n)";
        insert.Parameters.AddWithValue("$id", tag.Id);
        insert.Parameters.AddWithValue("$n", tag.Name);
        insert.ExecuteNonQuery();
        return tag;
    }

    public IReadOnlyList<Tag> All()
    {
        List<Tag> list = new();
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, name FROM tags ORDER BY name";
        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Tag { Id = r.GetString(0), Name = r.GetString(1) });
        }
        return list;
    }

    public IReadOnlyList<Tag> ForNote(string noteId)
    {
        List<Tag> list = new();
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = @"
SELECT t.id, t.name
FROM tags t
JOIN note_tags nt ON nt.tag_id = t.id
WHERE nt.note_id = $id
ORDER BY t.name";
        cmd.Parameters.AddWithValue("$id", noteId);
        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Tag { Id = r.GetString(0), Name = r.GetString(1) });
        }
        return list;
    }

    public void Attach(string noteId, string tagId)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO note_tags(note_id, tag_id) VALUES ($note, $tag)";
        cmd.Parameters.AddWithValue("$note", noteId);
        cmd.Parameters.AddWithValue("$tag", tagId);
        cmd.ExecuteNonQuery();
    }

    public void Detach(string noteId, string tagId)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM note_tags WHERE note_id = $note AND tag_id = $tag";
        cmd.Parameters.AddWithValue("$note", noteId);
        cmd.Parameters.AddWithValue("$tag", tagId);
        cmd.ExecuteNonQuery();
    }

    public void ReplaceForNote(string noteId, IEnumerable<string> tagNames)
    {
        using SqliteTransaction tx = _db.Connection.BeginTransaction();
        using (SqliteCommand del = _db.Connection.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM note_tags WHERE note_id = $id";
            del.Parameters.AddWithValue("$id", noteId);
            del.ExecuteNonQuery();
        }
        foreach (string name in tagNames)
        {
            string normalized = Normalize(name);
            if (normalized.Length == 0) continue;
            Tag tag = GetOrCreate(normalized);
            using SqliteCommand attach = _db.Connection.CreateCommand();
            attach.Transaction = tx;
            attach.CommandText = "INSERT OR IGNORE INTO note_tags(note_id, tag_id) VALUES ($note, $tag)";
            attach.Parameters.AddWithValue("$note", noteId);
            attach.Parameters.AddWithValue("$tag", tag.Id);
            attach.ExecuteNonQuery();
        }
        tx.Commit();
    }
}
