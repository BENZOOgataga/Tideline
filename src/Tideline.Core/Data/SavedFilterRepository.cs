using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Tideline.Core.Models;

namespace Tideline.Core.Data;

public sealed class SavedFilterRepository
{
    private readonly NotesDb _db;

    public SavedFilterRepository(NotesDb db)
    {
        _db = db;
    }

    public SavedFilter Create(string name, string query, bool feedsBriefing = false)
    {
        SavedFilter f = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Query = query,
            FeedsBriefing = feedsBriefing,
        };
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "INSERT INTO saved_filters(id, name, query, feeds_briefing) VALUES ($id, $n, $q, $b)";
        cmd.Parameters.AddWithValue("$id", f.Id);
        cmd.Parameters.AddWithValue("$n", f.Name);
        cmd.Parameters.AddWithValue("$q", f.Query);
        cmd.Parameters.AddWithValue("$b", f.FeedsBriefing ? 1 : 0);
        cmd.ExecuteNonQuery();
        return f;
    }

    public IReadOnlyList<SavedFilter> All()
    {
        List<SavedFilter> list = new();
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, query, feeds_briefing FROM saved_filters ORDER BY name COLLATE NOCASE";
        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new SavedFilter
            {
                Id = r.GetString(0),
                Name = r.GetString(1),
                Query = r.GetString(2),
                FeedsBriefing = r.GetInt64(3) != 0,
            });
        }
        return list;
    }

    public void Delete(string id)
    {
        using SqliteCommand cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM saved_filters WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}
