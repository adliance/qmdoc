using System.IO;
using Markdig;
using Markdig.Renderers.Docx;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adliance.QmDoc;

public class MarkdownToDocxConverter
{
    public static void ConvertHtmlToDocx(string baseDirectory, string sourceFilePath, string targetPath)
    {
        var markdown = File.ReadAllText(sourceFilePath);

        var document = DocxTemplateHelper.Standard;
        var styles = new DocumentStyles();
        var renderer = new DocxDocumentRenderer(document, styles, NullLogger<DocxDocumentRenderer>.Instance);

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        Markdown.Convert(markdown, renderer, pipeline);

        document.SaveAs(targetPath);
    }
}