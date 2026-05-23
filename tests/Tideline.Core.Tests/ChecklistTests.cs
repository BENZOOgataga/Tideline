using Tideline.Core.Parsing;
using Xunit;

namespace Tideline.Core.Tests;

public class ChecklistTests
{
    [Fact]
    public void Parses_unchecked_and_checked_lines()
    {
        string body = "buy groceries\n- [ ] bread\n- [x] milk\n- [X] eggs";
        var items = ChecklistParser.Parse(body);
        Assert.Equal(3, items.Count);
        Assert.False(items[0].Done);
        Assert.True(items[1].Done);
        Assert.True(items[2].Done);
        Assert.Equal("bread", items[0].Text);
    }

    [Fact]
    public void Progress_counts_done_over_total()
    {
        string body = "- [ ] a\n- [x] b\n- [x] c";
        var (done, total) = ChecklistParser.Progress(body);
        Assert.Equal(2, done);
        Assert.Equal(3, total);
    }

    [Fact]
    public void Toggle_flips_and_preserves_other_lines()
    {
        string body = "intro\n- [ ] item one\n- [x] item two\nouter";
        string toggled = ChecklistParser.Toggle(body, 1);
        Assert.Contains("- [x] item one", toggled);
        Assert.Contains("- [x] item two", toggled);
        Assert.Contains("intro", toggled);

        toggled = ChecklistParser.Toggle(toggled, 2);
        Assert.Contains("- [ ] item two", toggled);
    }

    [Fact]
    public void Body_without_checklist_returns_empty()
    {
        var items = ChecklistParser.Parse("plain note");
        Assert.Empty(items);
    }
}
