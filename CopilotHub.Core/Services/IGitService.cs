using CopilotHub.Core.Models;

namespace CopilotHub.Core.Services;

public interface IGitService
{
    bool IsGitRepository(string directoryPath);
    IEnumerable<string> GetUnstagedChanges(string repositoryPath);
    FileDiffResult? GetFileDiff(string repositoryPath, string relativeFilePath);
    string? GetFileContentAtHead(string repositoryPath, string relativeFilePath);
}
