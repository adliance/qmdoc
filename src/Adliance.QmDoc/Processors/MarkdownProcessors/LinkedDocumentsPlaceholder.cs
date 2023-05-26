using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class LinkedDocumentsPlaceholder : IMarkdownProcessor
{
    private readonly string _sourceFilePath;

    public LinkedDocumentsPlaceholder(string sourceFilePath)
    {
        _sourceFilePath = sourceFilePath;
    }

    public MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext)
    {
        var pattern = @"\{\{\W*LINKED_DOCUMENTS\W*\}\}";
        var result = markdown;

        if (Regex.IsMatch(markdown, pattern, RegexOptions.IgnoreCase))
        {
            var replacement = markdownProcessorContext.LinkedDocuments.Distinct().OrderBy(x => x.NiceName).Aggregate("", (current, d) => current + $"{Environment.NewLine}* <span class=\"link-to-document\"><i></i>[{d.NiceName}]({d.FileName})</span>");
            result = Regex.Replace(result, pattern, replacement.Trim(), RegexOptions.IgnoreCase);
        }

        return new MarkdownProcessorResult(result, markdownProcessorContext);
    }
}