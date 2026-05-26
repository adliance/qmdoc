using System.Collections.Generic;
using System.Threading.Tasks;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Markdig;
using Markdig.Renderers.Docx;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adliance.QmDoc.Converter.Docx;

public class DocxConverter(DocxParameters parameters, Options.Options options) : Converter(TargetExtension.Docx, parameters, options)
{
    protected override async Task<byte[]> Convert(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        var document = DocxTemplateHelper.Standard;
        var styles = new DocumentStyles();
        var renderer = new DocxDocumentRenderer(document, styles, NullLogger<DocxDocumentRenderer>.Instance);
        renderer.ObjectRenderers.Add(new TableRenderer());
        if (renderer.ObjectRenderers is IList<Markdig.Renderers.IMarkdownObjectRenderer> list)
            list.Insert(0, new PageBreakRenderer());
        else
            renderer.ObjectRenderers.Add(new PageBreakRenderer());

        Markdown.Convert(markdownContext.Markdown, renderer, markdownContext.Pipeline);
        IconBlocksProcessor.Apply(document);
        AuthorLineProcessor.Apply(document);
        var title = ApplyCommonPlaceholders(file, "{{ TITLE }}", markdownContext);
        TitleBlockProcessor.Apply(document, title);
        FooterProcessor.Apply(document, title, ApplyCommonPlaceholders(file, "{{ GIT_DATE_VERSION }}", markdownContext));

        var tempPath = System.IO.Path.GetTempFileName();
        document.SaveAs(tempPath).Close();

        var bytes = await System.IO.File.ReadAllBytesAsync(tempPath);
        System.IO.File.Delete(tempPath);
        return bytes;
    }

    protected override void PrepareAdditionalProcessors(ConverterFile file, IList<IMarkdownProcessor> markdownProcessors)
    {
        // do nothing here, no special DOCX processors
    }
}
