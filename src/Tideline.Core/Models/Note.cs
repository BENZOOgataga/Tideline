namespace Tideline.Core.Models;

public sealed class Note
{
    public string Id { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public long? RemindAt { get; set; }
    public long? DueAt { get; set; }
    public string? Recurrence { get; set; }
    public bool Archived { get; set; }
    public long? ArchivedAt { get; set; }
    public string? SpaceId { get; set; }
    public int SnoozeCount { get; set; }
    public bool Pinned { get; set; }
}

public sealed class Space
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? NorthStarNoteId { get; set; }
    public bool Archived { get; set; }
    public long CreatedAt { get; set; }
}

public sealed class Tag
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class Attachment
{
    public string Id { get; set; } = string.Empty;
    public string NoteId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string PathOrUrl { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public long AddedAt { get; set; }
}

public sealed class SavedFilter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public bool FeedsBriefing { get; set; }
}

public static class AttachmentKinds
{
    public const string ImageCopied = "image_copied";
    public const string FileRef = "file_ref";
    public const string FolderRef = "folder_ref";
    public const string Url = "url";
}
