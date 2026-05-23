using System.Collections.Generic;

namespace Tideline.Core.Filtering;

public enum TagMatchMode
{
    All,
    Any,
}

public enum DueRange
{
    Any,
    None,
    Today,
    ThisWeek,
    Overdue,
}

public sealed class FilterQuery
{
    public List<string> Tags { get; } = new();
    public TagMatchMode TagMode { get; set; } = TagMatchMode.All;
    public string? SpaceName { get; set; }
    public DueRange Due { get; set; } = DueRange.Any;
    public string? Text { get; set; }
    public bool IncludeArchived { get; set; }

    public bool IsEmpty =>
        Tags.Count == 0 &&
        string.IsNullOrEmpty(SpaceName) &&
        Due == DueRange.Any &&
        string.IsNullOrEmpty(Text) &&
        !IncludeArchived;
}
