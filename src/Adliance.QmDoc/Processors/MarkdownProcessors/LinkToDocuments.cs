using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class LinkToDocuments(string baseDirectory, string filePath) : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        ReplaceDocumentsByFileExtension(markdownContext);
        ReplaceDocumentsByCode(markdownContext);
        return markdownContext;
    }

    private void ReplaceDocumentsByCode(MarkdownProcessorContext markdownContext)
    {
        var matches = Regex.Matches(markdownContext.Markdown, @"\[(\w\w\w\-\d\d\d)\]");

        foreach (Match? m in matches)
        {
            if (m == null) continue;

            var code = m.Groups[1].Value;

            var allFiles = Directory.GetFiles(baseDirectory, "*.md", SearchOption.AllDirectories).ToList();
            var linkedFilePath = allFiles.FirstOrDefault(x => Path.GetFileName(x).StartsWith(code, true, CultureInfo.InvariantCulture));

            // we prefer *.md files ... but if we can't find a matching one, we just take whatever
            if (linkedFilePath == null)
            {
                allFiles = Directory.GetFiles(baseDirectory, "*.*", SearchOption.AllDirectories).ToList();
                linkedFilePath = allFiles.FirstOrDefault(x => Path.GetFileName(x).StartsWith(code, true, CultureInfo.InvariantCulture));
            }

            if (linkedFilePath == null)
            {
                markdownContext.Errors.Add(new ProcessorError(filePath, $"Unable to find a document \"{code}\", but there's a referenced document number to it."));
            }
            else
            {
                var relativeFilePath = Path.GetRelativePath(Path.GetDirectoryName(filePath)!, linkedFilePath).Replace("\\", "/");
                var targetFilePath = relativeFilePath.Replace(" ", "%20").Replace(".md", ".html");
                var linkedDocument = new LinkedDocument(targetFilePath, Path.GetFileNameWithoutExtension(relativeFilePath));
                markdownContext.LinkedDocuments.Add(linkedDocument);
                markdownContext.Markdown = markdownContext.Markdown.Replace($"[{code}]", $"<span class=\"link-to-document\"><i></i>[{linkedDocument.NiceName}]({linkedDocument.FileName})</span>");
            }
        }
    }

    private void ReplaceDocumentsByFileExtension(MarkdownProcessorContext markdownContext)
    {
        var matches = Regex.Matches(markdownContext.Markdown, @"\[([^\]]*?\.\w{1,4})\]");

        foreach (Match? m in matches)
        {
            if (m == null) continue;

            var linkedFileName = m.Groups[1].Value;
            linkedFileName = linkedFileName.Replace("%20", " ");
            if (linkedFileName.Contains('@', StringComparison.OrdinalIgnoreCase)) continue;

            var matchingFile = new FileInfo(Path.Combine(Path.GetDirectoryName(filePath)!, linkedFileName));

            if (!matchingFile.Exists)
            {
                var allFiles = Directory.GetFiles(Path.GetDirectoryName(baseDirectory) ?? "", "*.md", SearchOption.AllDirectories).ToList();
                var matchingFiles = allFiles.Where(x => Path.GetFileName(x).Equals(linkedFileName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matchingFiles.Any()) matchingFile = new FileInfo(matchingFiles.First());
            }

            if (!matchingFile.Exists)
            {
                markdownContext.Errors.Add(new ProcessorError(filePath, $"Unable to find a document \"{linkedFileName}\", but there's a reference to it."));
            }
            else
            {
                var fileName = linkedFileName;

                if (Path.GetExtension(fileName).Equals(".md", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Replace(".md", ".html");
                }

                var linkedDocument = new LinkedDocument(fileName.Replace(" ", "%20"), Path.GetFileNameWithoutExtension(fileName.Replace("%20", " ")));
                markdownContext.LinkedDocuments.Add(linkedDocument);
                markdownContext.Markdown = markdownContext.Markdown.Replace($"[{linkedFileName}]", $"<span class=\"link-to-document\"><i></i>[{linkedDocument.NiceName}]({linkedDocument.FileName})</span>");
            }
        }
    }
}
