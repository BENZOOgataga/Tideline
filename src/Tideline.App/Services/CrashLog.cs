using System;
using System.IO;
using Tideline.Core.Data;

namespace Tideline.App.Services;

/// <summary>
/// Best-effort crash log to a file under the app data directory. Used by
/// App's unhandled exception hooks; nothing in here is allowed to throw.
/// </summary>
public static class CrashLog
{
    public static string FilePath
    {
        get
        {
            try { return Path.Combine(AppPaths.Root, "crash.log"); }
            catch { return Path.Combine(Path.GetTempPath(), "tideline-crash.log"); }
        }
    }

    public static void Write(string source, Exception? ex)
    {
        try
        {
            AppPaths.EnsureDirectories();
            string line =
                $"[{DateTimeOffset.Now:O}] {source}: {ex?.GetType().FullName} {ex?.Message}\n{ex}\n----\n";
            File.AppendAllText(FilePath, line);
        }
        catch
        {
            // last resort: swallow
        }
    }
}
