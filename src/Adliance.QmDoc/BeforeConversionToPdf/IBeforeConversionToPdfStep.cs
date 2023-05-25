using System.Collections.Generic;

namespace Adliance.QmDoc.BeforeConversionToPdf;

internal interface IBeforeConversionToPdfStep
{
    Result Apply(string html);
}

public class Result
{
    public Result(string resultingHtml)
    {
        ResultingHtml = resultingHtml;
    }

    public string ResultingHtml { get; set; }
    public IList<ProcessorError> Errors { get; set; } = new List<ProcessorError>();
}