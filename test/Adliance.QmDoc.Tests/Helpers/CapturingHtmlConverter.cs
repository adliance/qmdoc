using Adliance.QmDoc.Converter;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;

namespace Adliance.QmDoc.Tests.Helpers;

/// <summary>
/// An <see cref="HtmlConverter"/> that remembers the processed Markdown (including its errors) of every converted file.
/// </summary>
public sealed class CapturingHtmlConverter(HtmlParameters parameters, bool saveMarkdown = false) : HtmlConverter(parameters, new Options.Options(), saveMarkdown)
{
    public List<(ConverterFile File, MarkdownProcessorContext Context)> Results { get; } = [];

    public MarkdownProcessorContext SingleContext => Assert.Single(Results).Context;

    protected override async Task<byte[]> Convert(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        var result = await base.Convert(file, markdownContext);
        Results.Add((file, markdownContext));
        return result;
    }
}
