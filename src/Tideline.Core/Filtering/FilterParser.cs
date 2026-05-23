using System;
using System.Collections.Generic;
using System.Text;
using Tideline.Core.Data;

namespace Tideline.Core.Filtering;

/// <summary>
/// Parses the inline filter language described in SPEC section 9, for example
/// <c>#work #urgent due:thisweek</c> or <c>any:#idea #someday space:learning</c>.
/// Free text outside tokens becomes the FTS text term.
/// </summary>
public static class FilterParser
{
    public static FilterQuery Parse(string input)
    {
        FilterQuery q = new();
        if (string.IsNullOrWhiteSpace(input)) return q;

        StringBuilder text = new();
        string[] tokens = input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string raw in tokens)
        {
            string t = raw.Trim();
            if (t.Length == 0) continue;

            if (t.StartsWith("any:", StringComparison.OrdinalIgnoreCase))
            {
                q.TagMode = TagMatchMode.Any;
                string rest = t.Substring(4);
                AppendTagsAndFlags(rest, q);
                continue;
            }
            if (t.StartsWith("all:", StringComparison.OrdinalIgnoreCase))
            {
                q.TagMode = TagMatchMode.All;
                AppendTagsAndFlags(t.Substring(4), q);
                continue;
            }
            if (t.StartsWith("#"))
            {
                string name = TagRepository.Normalize(t);
                if (name.Length > 0) q.Tags.Add(name);
                continue;
            }
            if (t.StartsWith("space:", StringComparison.OrdinalIgnoreCase))
            {
                q.SpaceName = t.Substring(6).Trim('"');
                continue;
            }
            if (t.StartsWith("due:", StringComparison.OrdinalIgnoreCase))
            {
                q.Due = ParseDue(t.Substring(4));
                continue;
            }
            if (t.Equals("archived", StringComparison.OrdinalIgnoreCase) || t.Equals("is:archived", StringComparison.OrdinalIgnoreCase))
            {
                q.IncludeArchived = true;
                continue;
            }
            if (text.Length > 0) text.Append(' ');
            text.Append(t);
        }
        q.Text = text.Length > 0 ? text.ToString() : null;
        return q;
    }

    private static void AppendTagsAndFlags(string rest, FilterQuery q)
    {
        if (string.IsNullOrEmpty(rest)) return;
        foreach (string piece in rest.Split(','))
        {
            string name = TagRepository.Normalize(piece);
            if (name.Length > 0) q.Tags.Add(name);
        }
    }

    private static DueRange ParseDue(string s) => s.ToLowerInvariant() switch
    {
        "today" => DueRange.Today,
        "thisweek" or "this-week" => DueRange.ThisWeek,
        "overdue" or "late" => DueRange.Overdue,
        "none" or "no" => DueRange.None,
        _ => DueRange.Any,
    };
}
