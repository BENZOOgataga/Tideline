using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Tideline.Core.Models;
using Tideline.Core.Time;

namespace Tideline.Core.Data;

public sealed class SpaceRepository
{
    private readonly NotesDb _db;
    private readonly IClock _clock;

    public SpaceRepository(NotesDb db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public Space Create(string name, string? color = null)
    {
        Space space = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Color = color,
            CreatedAt = _clock.NowMs(),
        };
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "INSERT INTO spaces(id, name, color, north_star_note_id, archived, created_at) VALUES ($id, $name, $color, NULL, 0, $createdAt)";
        cmd.Parameters.AddWithValue("$id", space.Id);
        cmd.Parameters.AddWithValue("$name", space.Name);
        cmd.Parameters.AddWithValue("$color", (object?)space.Color ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$createdAt", space.CreatedAt);
        cmd.ExecuteNonQuery();
        return space;
    }

    public IReadOnlyList<Space> All(bool includeArchived = false)
    {
        List<Space> list = new();
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = includeArchived
            ? "SELECT id, name, color, north_star_note_id, archived, created_at FROM spaces ORDER BY name COLLATE NOCASE"
            : "SELECT id, name, color, north_star_note_id, archived, created_at FROM spaces WHERE archived = 0 ORDER BY name COLLATE NOCASE";
        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Space
            {
                Id = r.GetString(0),
                Name = r.GetString(1),
                Color = r.IsDBNull(2) ? null : r.GetString(2),
                NorthStarNoteId = r.IsDBNull(3) ? null : r.GetString(3),
                Archived = r.GetInt64(4) != 0,
                CreatedAt = r.GetInt64(5),
            });
        }
        return list;
    }

    public Space? GetById(string id)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, color, north_star_note_id, archived, created_at FROM spaces WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using SqliteDataReader r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new Space
        {
            Id = r.GetString(0),
            Name = r.GetString(1),
            Color = r.IsDBNull(2) ? null : r.GetString(2),
            NorthStarNoteId = r.IsDBNull(3) ? null : r.GetString(3),
            Archived = r.GetInt64(4) != 0,
            CreatedAt = r.GetInt64(5),
        };
    }

    public void Rename(string id, string name)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE spaces SET name = $n WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$n", name);
        cmd.ExecuteNonQuery();
    }

    public void SetColor(string id, string? color)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE spaces SET color = $c WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$c", (object?)color ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void SetNorthStar(string id, string? noteId)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE spaces SET north_star_note_id = $n WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$n", (object?)noteId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Archiving a Space sweeps its notes into the archive but keeps them
    /// searchable (SPEC section 7, edge case "Archiving a Space").
    /// </summary>
    public void Archive(string id)
    {
        long now = _clock.NowMs();
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE spaces SET archived = 1 WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();

        using SqliteCommand sweep = _db.Connection.CreateCommand();
        sweep.CommandText = "UPDATE notes SET archived = 1, archived_at = $now, updated_at = $now WHERE space_id = $id AND archived = 0";
        sweep.Parameters.AddWithValue("$id", id);
        sweep.Parameters.AddWithValue("$now", now);
        sweep.ExecuteNonQuery();
    }

    public void Restore(string id)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE spaces SET archived = 0 WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}
