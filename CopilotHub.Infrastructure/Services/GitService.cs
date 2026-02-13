using CopilotHub.Core.Models;
using CopilotHub.Core.Services;
using LibGit2Sharp;
using Serilog;

namespace CopilotHub.Infrastructure.Services;

public class GitService : IGitService
{
    private readonly ILogger _logger = Log.ForContext<GitService>();

    public bool IsGitRepository(string directoryPath)
    {
        try
        {
            return Repository.IsValid(directoryPath);
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<string> GetUnstagedChanges(string repositoryPath)
    {
        if (!IsGitRepository(repositoryPath))
            return [];

        try
        {
            using var repo = new Repository(repositoryPath);
            var status = repo.RetrieveStatus(new StatusOptions());

            return status
                .Where(s => s.State != FileStatus.Ignored && s.State != FileStatus.Unaltered)
                .Select(s => s.FilePath)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting unstaged changes for {Path}", repositoryPath);
            return [];
        }
    }

    public FileDiffResult? GetFileDiff(string repositoryPath, string relativeFilePath)
    {
        if (!IsGitRepository(repositoryPath))
            return null;

        try
        {
            var originalContent = GetFileContentAtHead(repositoryPath, relativeFilePath) ?? string.Empty;
            var currentPath = Path.Combine(repositoryPath, relativeFilePath);
            var modifiedContent = File.Exists(currentPath) ? File.ReadAllText(currentPath) : string.Empty;

            return new FileDiffResult(relativeFilePath, originalContent, modifiedContent, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting diff for {File} in {Repo}", relativeFilePath, repositoryPath);
            return null;
        }
    }

    public string? GetFileContentAtHead(string repositoryPath, string relativeFilePath)
    {
        if (!IsGitRepository(repositoryPath))
            return null;

        try
        {
            using var repo = new Repository(repositoryPath);
            var head = repo.Head?.Tip;
            if (head is null) return null;

            var treeEntry = head[relativeFilePath];
            if (treeEntry?.Target is not Blob blob) return null;

            return blob.GetContentText();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting HEAD content for {File}", relativeFilePath);
            return null;
        }
    }
}
