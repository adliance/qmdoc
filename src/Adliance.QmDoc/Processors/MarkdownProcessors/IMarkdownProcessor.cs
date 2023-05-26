using System;
using System.Collections.Generic;

namespace Adliance.QmDoc.Processors.MarkdownProcessors;

public interface IMarkdownProcessor
{
    MarkdownProcessorResult Apply(string markdown, MarkdownProcessorContext markdownProcessorContext);
}

public class MarkdownProcessorResult
{
    public MarkdownProcessorResult(string resultingMarkdown, MarkdownProcessorContext markdownProcessorContext)
    {
        MarkdownProcessorContext = markdownProcessorContext;
        ResultingMarkdown = resultingMarkdown;
    }

    public string ResultingMarkdown { get; set; }
    public IList<ProcessorError> Errors { get; set; } = new List<ProcessorError>();
    public MarkdownProcessorContext MarkdownProcessorContext { get; set; }
}

public class MarkdownProcessorContext
{
    public IList<LinkedDocument> LinkedDocuments { get; set; } = new List<LinkedDocument>();
}

public class LinkedDocument
{
    public LinkedDocument(string fileName, string niceName)
    {
        FileName = fileName;
        NiceName = niceName;
    }

    public string FileName { get; set; }
    public string NiceName { get; set; }

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