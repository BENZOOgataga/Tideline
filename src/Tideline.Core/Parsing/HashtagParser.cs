using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tideline.Core.Parsing;

/// <summary>
/// Extracts <c>#hashtag</c> tokens from a note body for inline tag parsing
/// (SPEC sections 7 and 9). Tag matching is lowercase-normalized and
/// limited to Unicode letters, digits, underscore, and hyphen so it stays
/// predictable across keyboards.
/// </summary>
public static class HashtagParser
{
    private static readonly Regex Pattern = new(
        @"(?<![\w/])#([\p{L}\p{N}_-]{1,64})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> Extract(string body)
    {
        List<string> tags = new();
        if (string.IsNullOrWhiteSpace(body)) return tags;
        HashSet<string> seen = new();
        foreach (Match m in Pattern.Matches(body))
        {
            string name = m.Groups[1].Value.ToLowerInvariant();
            if (seen.Add(name))
            {
                tags.Add(name);
            }
        }
        return tags;
    }
}
