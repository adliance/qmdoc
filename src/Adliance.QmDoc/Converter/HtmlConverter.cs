using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;

namespace Adliance.QmDoc.Converter;

public class HtmlConverter(ThemedConversionParameters parameters, Options.Options options) : Converter(TargetExtension.Html, parameters, options)
{
    protected override Task<byte[]> Convert(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        var html = RunHtmlProcessors(file, markdownContext);
        return Task.FromResult(Encoding.UTF8.GetBytes(html));
    }

    protected override void PrepareAdditionalProcessors(ConverterFile file, IList<IMarkdownProcessor> markdownProcessors)
    {
        markdownProcessors.Add(new LinkToChapters());
        markdownProcessors.Add(new PageBreak());
        markdownProcessors.Add(new LinkToDocuments(file.SourceBaseDirectory, file.SourceAbsolutePath));
        markdownProcessors.Add(new LinkedDocumentsPlaceholder()); // add after the "LinkToDocuments" step, because that one fills the context with the linked documents
    }
}
