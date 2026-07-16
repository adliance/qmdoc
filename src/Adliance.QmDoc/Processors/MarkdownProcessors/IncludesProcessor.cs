using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Options;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class IncludesProcessor(string sourceFilePath, string theme) : IMarkdownProcessor
{
    private static readonly Regex IncludeRegex = new(@"\{\{\s*include\s+([^{}]+?)\s*\}\}", RegexOptions.IgnoreCase);

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        markdownContext.Markdown = ResolveIncludes(markdownContext.Markdown, sourceFilePath, markdownContext, [sourceFilePath]);
        return markdownContext;
    }

    private string ResolveIncludes(string markdown, string currentFilePath, MarkdownProcessorContext markdownContext, HashSet<string> chain)
    {
        return IncludeRegex.Replace(markdown, m =>
        {
            var includePath = m.Groups[1].Value.Trim();
            var resolvedPath = ResolvePath(currentFilePath, includePath);

            if (resolvedPath == null)
            {
                markdownContext.Errors.Add(new ProcessorError(currentFilePath, $"Unable to find an include file \"{includePath}\", referenced from \"{Path.GetFileName(currentFilePath)}\"."));
                return m.Value;
            }

            if (!chain.Add(resolvedPath))
            {
                markdownContext.Errors.Add(new ProcessorError(currentFilePath, $"Circular include detected for \"{includePath}\", referenced from \"{Path.GetFileName(currentFilePath)}\"."));
                return "";
            }

            var content = File.ReadAllText(resolvedPath).Replace("\r\n", "\n");
            var result = ResolveIncludes(content, resolvedPath, markdownContext, chain);
            chain.Remove(resolvedPath);
            return result;
        });
    }

    private string? ResolvePath(string currentFilePath, string includePath)
    {
        var relativeToCurrentFile = Path.Combine(Path.GetDirectoryName(currentFilePath) ?? "", includePath);
        if (File.Exists(relativeToCurrentFile)) return new FileInfo(relativeToCurrentFile).FullName;

        var relativeToTheme = Path.Combine(OptionsProvider.DataDirectory, "themes", theme, includePath);
        if (File.Exists(relativeToTheme)) return new FileInfo(relativeToTheme).FullName;

        return null;
    }
}
