using System;
using System.IO;

namespace Tideline.Core.Data;

public static class AppPaths
{
    // Debug builds use a separate folder so a dev run never touches the
    // installed app's notes.db / attachments / theme. TIDELINE_DATA_DIR
    // still overrides both when set (used by the harness for isolation).
#if DEBUG
    public const string FolderName = "Tideline-dev";
#else
    public const string FolderName = "Tideline";
#endif
    public const string DbFileName = "notes.db";
    public const string AttachmentsFolderName = "attachments";

    public static string Root
    {
        get
        {
            string overrideDir = Environment.GetEnvironmentVariable("TIDELINE_DATA_DIR") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(overrideDir))
            {
                return overrideDir;
            }
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(local, FolderName);
        }
    }

    public static string DatabasePath => Path.Combine(Root, DbFileName);
    public static string AttachmentsDirectory => Path.Combine(Root, AttachmentsFolderName);

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(AttachmentsDirectory);
    }
}
