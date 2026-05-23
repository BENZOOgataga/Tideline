using System;
using System.IO;

namespace Tideline.Core.Data;

public static class AppPaths
{
    public const string FolderName = "Tideline";
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
