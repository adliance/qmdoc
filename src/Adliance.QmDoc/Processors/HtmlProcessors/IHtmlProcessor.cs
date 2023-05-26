using System.Collections.Generic;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

internal interface IHtmlProcessor
{
    HtmlProcessorResult Apply(string html);
}

public class HtmlProcessorResult
{
    public HtmlProcessorResult(string resultingHtml)
    {
        ResultingHtml = resultingHtml;
    }

    public string ResultingHtml { get; set; }
    public IList<ProcessorError> Errors { get; set; } = new List<ProcessorError>();
}