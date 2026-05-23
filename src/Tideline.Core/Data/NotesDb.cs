using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Tideline.Core.Data;

public sealed class NotesDb : IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public string DatabasePath { get; }
    public SqliteConnection Connection => _connection;

    private NotesDb(string path, SqliteConnection connection)
    {
        DatabasePath = path;
        _connection = connection;
    }

    public static NotesDb OpenDefault()
    {
        AppPaths.EnsureDirectories();
        return Open(AppPaths.DatabasePath);
    }

    public static NotesDb Open(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        SqliteConnection conn = new(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true,
        }.ToString());
        conn.Open();
        NotesDb db = new(path, conn);
        db.Initialize();
        return db;
    }

    private void Initialize()
    {
        using SqliteCommand schema = _connection.CreateCommand();
        schema.CommandText = Schema.Ddl;
        schema.ExecuteNonQuery();

        long current = ReadSchemaVersion();
        if (current == 0)
        {
            using SqliteCommand insert = _connection.CreateCommand();
            insert.CommandText = "INSERT INTO schema_version(version) VALUES ($v)";
            insert.Parameters.AddWithValue("$v", Schema.CurrentVersion);
            insert.ExecuteNonQuery();
        }
        else if (current < Schema.CurrentVersion)
        {
            // Reserved for future migrations.
            using SqliteCommand bump = _connection.CreateCommand();
            bump.CommandText = "UPDATE schema_version SET version = $v";
            bump.Parameters.AddWithValue("$v", Schema.CurrentVersion);
            bump.ExecuteNonQuery();
        }
    }

    private long ReadSchemaVersion()
    {
        using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_version";
        object? value = cmd.ExecuteScalar();
        return value is long l ? l : Convert.ToInt64(value ?? 0L);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Dispose();
    }
}
