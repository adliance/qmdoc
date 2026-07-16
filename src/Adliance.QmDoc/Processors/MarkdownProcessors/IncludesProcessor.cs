using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Options;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class IncludesProcessor(string sourceFilePath, string theme) : IMarkdownProcessor
{
    private static readonly Regex IncludeRegex = new(@"\{\{\s*include\s+([^{}]+?)\s*\}\}", RegexOptions.IgnoreCase);
    private static readonly Regex LanguageCodeRegex = new(@"^[a-z]{2,3}(-[a-z]{2})?$", RegexOptions.IgnoreCase);

    private readonly string? _language = DetectLanguage(sourceFilePath);

    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        markdownContext.Markdown = ResolveIncludes(markdownContext.Markdown, sourceFilePath, markdownContext, [sourceFilePath]);
        return markdownContext;
    }

    /// <summary>
    /// Detects the language code of a document named "&lt;filename&gt;.&lt;language_iso_code&gt;.md" (eg. "report.de.md").
    /// </summary>
    private static string? DetectLanguage(string filePath)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var languageCandidate = Path.GetExtension(fileNameWithoutExtension).TrimStart('.');
        return LanguageCodeRegex.IsMatch(languageCandidate) ? languageCandidate : null;
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
        var candidates = GetCandidatePaths(includePath).ToList();
        var currentFileDirectory = Path.GetDirectoryName(currentFilePath) ?? "";

        foreach (var candidate in candidates)
        {
            var relativeToCurrentFile = Path.Combine(currentFileDirectory, candidate);
            if (File.Exists(relativeToCurrentFile)) return new FileInfo(relativeToCurrentFile).FullName;
        }

        foreach (var candidate in candidates)
        {
            var relativeToTheme = Path.Combine(OptionsProvider.DataDirectory, "themes", theme, candidate);
            if (File.Exists(relativeToTheme)) return new FileInfo(relativeToTheme).FullName;
        }

        return null;
    }

    /// <summary>
    /// Returns the include path variants to look for, in priority order: the language-specific
    /// version first (eg. "partial.de.md" if the document being converted is "report.de.md"),
    /// falling back to the plain, non-translated path.
    /// </summary>
    private IEnumerable<string> GetCandidatePaths(string includePath)
    {
        if (!string.IsNullOrEmpty(_language))
        {
            var directory = Path.GetDirectoryName(includePath) ?? "";
            var fileName = Path.GetFileNameWithoutExtension(includePath);
            var extension = Path.GetExtension(includePath);
            yield return Path.Combine(directory, $"{fileName}.{_language}{extension}");
        }

        yield return includePath;
    }
}
