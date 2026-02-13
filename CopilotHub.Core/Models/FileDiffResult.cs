namespace CopilotHub.Core.Models;

public record FileDiffResult(
    string FilePath,
    string OriginalContent,
    string ModifiedContent,
    string UnifiedDiff);
