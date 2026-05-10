using System;
using System.Collections.Generic;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public interface IMarkdownProcessor
{
    MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext);
}

public class MarkdownProcessorResult(string resultingMarkdown, MarkdownProcessorContext markdownProcessorContext)
{
    public string ResultingMarkdown { get; set; } = resultingMarkdown;
    public IList<ProcessorError> Errors { get; set; } = new List<ProcessorError>();
    public MarkdownProcessorContext MarkdownProcessorContext { get; set; } = markdownProcessorContext;
}

public class MarkdownProcessorContext
{
    public IList<LinkedDocument> LinkedDocuments { get; set; } = new List<LinkedDocument>();
}

public class LinkedDocument(string fileName, string niceName)
{
    public string FileName { get; set; } = fileName;
    public string NiceName { get; set; } = niceName;

    public override bool Equals(object? obj)
    {
        if (obj is LinkedDocument o)
        {
            return (FileName + NiceName).Equals(o.FileName + o.NiceName, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (FileName + NiceName).ToLower().GetHashCode();
    }
}
