using System.Linq;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class LinkedDocumentsPlaceholder : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        var replacement = markdownContext.LinkedDocuments.Distinct().OrderBy(x => x.NiceName).Aggregate("", (current, d) => current + $"\n* <span class=\"link-to-document\"><i></i>[{d.NiceName}]({d.FileName})</span>");
        markdownContext.ReplacePlaceholder("LINKED_DOCUMENTS", replacement.Trim());
        return markdownContext;
    }
}
