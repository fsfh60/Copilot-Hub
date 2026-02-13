using CopilotHub.Core.Models;

namespace CopilotHub.Core.Services;

public interface IDiffService
{
    FileDiffResult ComputeDiff(string originalContent, string modifiedContent, string filePath);
}
