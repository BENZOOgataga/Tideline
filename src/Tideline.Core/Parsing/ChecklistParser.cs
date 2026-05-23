using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tideline.Core.Parsing;

/// <summary>
/// Parses the lightweight markdown checklist syntax described in SPEC section 5:
/// lines starting with <c>- [ ]</c> or <c>- [x]</c> become checklist items.
/// Plain text without checklist syntax returns an empty result and is unaffected.
/// </summary>
public static class ChecklistParser
{
    private static readonly Regex Line = new(
        @"^\s*[-*]\s*\[(?<state>[ xX])\]\s?(?<text>.*)$",
        RegexOptions.Compiled);

    public readonly record struct Item(int LineIndex, bool Done, string Text);

    public static IReadOnlyList<Item> Parse(string body)
    {
        List<Item> items = new();
        if (string.IsNullOrEmpty(body)) return items;
        string[] lines = body.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            Match m = Line.Match(lines[i].TrimEnd('\r'));
            if (m.Success)
            {
                bool done = m.Groups["state"].Value is "x" or "X";
                items.Add(new Item(i, done, m.Groups["text"].Value.Trim()));
            }
        }
        return items;
    }

    public static (int Done, int Total) Progress(string body)
    {
        var items = Parse(body);
        int done = 0;
        foreach (Item i in items)
        {
            if (i.Done) done++;
        }
        return (done, items.Count);
    }

    /// <summary>
    /// Toggles the given checklist line in place, preserving the rest of the body.
    /// Returns the new body, or the original if the line is not a checklist line.
    /// </summary>
    public static string Toggle(string body, int lineIndex)
    {
        string[] lines = body.Split('\n');
        if (lineIndex < 0 || lineIndex >= lines.Length) return body;
        string original = lines[lineIndex];
        string trimmed = original.TrimEnd('\r');
        string suffix = original.Length > trimmed.Length ? "\r" : string.Empty;
        Match m = Line.Match(trimmed);
        if (!m.Success) return body;
        bool wasDone = m.Groups["state"].Value is "x" or "X";
        int braceIndex = trimmed.IndexOf('[');
        if (braceIndex < 0 || braceIndex + 2 >= trimmed.Length) return body;
        char replacement = wasDone ? ' ' : 'x';
        string updated = trimmed.Substring(0, braceIndex + 1) + replacement + trimmed.Substring(braceIndex + 2);
        lines[lineIndex] = updated + suffix;
        return string.Join('\n', lines);
    }
}
