using CopilotHub.Infrastructure.Services;
using FluentAssertions;

namespace CopilotHub.Tests;

public class GitServiceTests
{
    private readonly GitService _sut = new();

    [Fact]
    public void IsGitRepository_NonExistentPath_ShouldReturnFalse()
    {
        _sut.IsGitRepository(@"C:\NonExistent_" + Guid.NewGuid().ToString("N"))
            .Should().BeFalse();
    }

    [Fact]
    public void IsGitRepository_TempDir_ShouldReturnFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            _sut.IsGitRepository(tempDir).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetUnstagedChanges_NonGitDir_ShouldReturnEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            _sut.GetUnstagedChanges(tempDir).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetFileDiff_NonGitDir_ShouldReturnNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            _sut.GetFileDiff(tempDir, "test.txt").Should().BeNull();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetFileContentAtHead_NonGitDir_ShouldReturnNull()
    {
        _sut.GetFileContentAtHead(@"C:\NonExistent", "test.txt")
            .Should().BeNull();
    }
}
