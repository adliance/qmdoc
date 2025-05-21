using System.Collections.Generic;

namespace Adliance.QmDoc.Processors.HtmlProcessors;

internal interface IHtmlProcessor
{
    HtmlProcessorResult Apply(string html);
}

public class HtmlProcessorResult(string resultingHtml)
{
    public string ResultingHtml { get; set; } = resultingHtml;
    public IList<ProcessorError> Errors { get; set; } = new List<ProcessorError>();
}