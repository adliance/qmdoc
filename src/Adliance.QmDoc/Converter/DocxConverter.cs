using System.Collections.Generic;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Markdig;
using Markdig.Renderers.Docx;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adliance.QmDoc.Converter;

public class DocxConverter : Converter
{
    public DocxConverter(DocxParameters parameters, Options.Options options) : base(TargetExtension.Docx, parameters, options)
    {
    }

    protected override byte[] Convert(ConverterFile file, string markdown)
    {
        var document = DocxTemplateHelper.Standard;
        var styles = new DocumentStyles();
        var renderer = new DocxDocumentRenderer(document, styles, NullLogger<DocxDocumentRenderer>.Instance);
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        Markdown.Convert(markdown, renderer, pipeline);

        var tempPath = System.IO.Path.GetTempFileName();
        document.SaveAs(tempPath).Close();

        var bytes = System.IO.File.ReadAllBytes(tempPath);
        System.IO.File.Delete(tempPath);
        return bytes;
    }

    protected override void PrepareAdditionalProcessors(ConverterFile file, IList<IMarkdownProcessor> markdownProcessors)
    {
        // do nothing here, no special DOCX processors
    }
}