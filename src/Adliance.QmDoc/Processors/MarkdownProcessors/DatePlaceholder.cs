using System;
using System.Globalization;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public class DatePlaceholder : IMarkdownProcessor
{
    public MarkdownProcessorContext Apply(MarkdownProcessorContext markdownContext)
    {
        markdownContext.ReplacePlaceholder("DATE", DateTime.Now.ToString("dd. MMMM yyyy", new CultureInfo("de-DE")));
        return markdownContext;
    }
}
