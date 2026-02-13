using System.Text;
using CopilotHub.Core.Models;
using CopilotHub.Core.Services;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace CopilotHub.Infrastructure.Services;

public class DiffService : IDiffService
{
    public FileDiffResult ComputeDiff(string originalContent, string modifiedContent, string filePath)
    {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(originalContent, modifiedContent);

        var sb = new StringBuilder();
        foreach (var line in diff.Lines)
        {
            var prefix = line.Type switch
            {
                ChangeType.Inserted => "+ ",
                ChangeType.Deleted => "- ",
                ChangeType.Modified => "~ ",
                _ => "  "
            };
            sb.AppendLine($"{prefix}{line.Text}");
        }

        return new FileDiffResult(filePath, originalContent, modifiedContent, sb.ToString());
    }
}
