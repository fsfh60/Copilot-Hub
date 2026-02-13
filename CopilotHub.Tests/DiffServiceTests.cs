using CopilotHub.Infrastructure.Services;
using FluentAssertions;

namespace CopilotHub.Tests;

public class DiffServiceTests
{
    private readonly DiffService _sut = new();

    [Fact]
    public void ComputeDiff_IdenticalContent_ShouldHaveNoDiffLines()
    {
        var content = "line1\nline2\nline3";
        var result = _sut.ComputeDiff(content, content, "test.txt");

        result.FilePath.Should().Be("test.txt");
        result.UnifiedDiff.Should().NotContain("+ ");
        result.UnifiedDiff.Should().NotContain("- ");
    }

    [Fact]
    public void ComputeDiff_AddedLines_ShouldShowInsertions()
    {
        var original = "line1\nline2";
        var modified = "line1\nline2\nline3";

        var result = _sut.ComputeDiff(original, modified, "test.txt");

        result.UnifiedDiff.Should().Contain("+ line3");
    }

    [Fact]
    public void ComputeDiff_RemovedLines_ShouldShowDeletions()
    {
        var original = "line1\nline2\nline3";
        var modified = "line1\nline3";

        var result = _sut.ComputeDiff(original, modified, "test.txt");

        result.UnifiedDiff.Should().Contain("- line2");
    }

    [Fact]
    public void ComputeDiff_EmptyOriginal_ShouldShowAllAsInsertions()
    {
        var modified = "line1\nline2";
        var result = _sut.ComputeDiff(string.Empty, modified, "test.txt");

        result.UnifiedDiff.Should().Contain("+ line1");
        result.UnifiedDiff.Should().Contain("+ line2");
    }

    [Fact]
    public void ComputeDiff_EmptyModified_ShouldShowAllAsDeletions()
    {
        var original = "line1\nline2";
        var result = _sut.ComputeDiff(original, string.Empty, "test.txt");

        result.UnifiedDiff.Should().Contain("- line1");
        result.UnifiedDiff.Should().Contain("- line2");
    }

    [Fact]
    public void ComputeDiff_ShouldReturnCorrectPaths()
    {
        var result = _sut.ComputeDiff("a", "b", "src/file.cs");
        result.FilePath.Should().Be("src/file.cs");
        result.OriginalContent.Should().Be("a");
        result.ModifiedContent.Should().Be("b");
    }
}
