using System.Collections.Generic;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Markdig;
using Markdig.Renderers.Docx;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adliance.QmDoc.Converter.Docx;

public class DocxConverter(DocxParameters parameters, Options.Options options) : Converter(TargetExtension.Docx, parameters, options)
{
    protected override byte[] Convert(ConverterFile file, string markdown)
    {
        var document = DocxTemplateHelper.Standard;
        var styles = new DocumentStyles();
        var renderer = new DocxDocumentRenderer(document, styles, NullLogger<DocxDocumentRenderer>.Instance);
        renderer.ObjectRenderers.Add(new TableRenderer());
        if (renderer.ObjectRenderers is System.Collections.Generic.IList<Markdig.Renderers.IMarkdownObjectRenderer> list)
            list.Insert(0, new PageBreakRenderer());
        else
            renderer.ObjectRenderers.Add(new PageBreakRenderer());
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        Markdown.Convert(markdown, renderer, pipeline);
        IconBlocksProcessor.Apply(document);
        FooterProcessor.Apply(document, ApplyCommonPlaceholders(file, "{{TITLE}}"), ApplyCommonPlaceholders(file, "{{GIT_DATE_VERSION}}"));

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
