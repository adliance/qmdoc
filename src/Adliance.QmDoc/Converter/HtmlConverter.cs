using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Humanizer;

namespace Adliance.QmDoc.Converter;

public class HtmlConverter(ThemedConversionParameters parameters, Options.Options options, bool saveMarkdown = false) : Converter(TargetExtension.Html, parameters, options)
{
    protected override async Task<byte[]> Convert(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        if (saveMarkdown) await SaveMarkdown(file, markdownContext);
        var html = RunHtmlProcessors(file, markdownContext);
        return Encoding.UTF8.GetBytes(html);
    }

    private static async Task SaveMarkdown(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        var markdownPath = Path.ChangeExtension(file.TargetAbsolutePath, ".md");
        if (string.Equals(Path.GetFullPath(markdownPath), Path.GetFullPath(file.SourceAbsolutePath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Saving the processed Markdown to {markdownPath} would overwrite the source file. Please specify a different --target directory.");
        }

        var bytes = Encoding.UTF8.GetBytes(markdownContext.Markdown);
        Program.WriteLine($"\tMD ({bytes.Length.Bytes().Humanize(CultureInfo.CurrentCulture)}) -> {markdownPath}");
        await File.WriteAllBytesAsync(markdownPath, bytes);
    }

    protected override void PrepareAdditionalProcessors(ConverterFile file, IList<IMarkdownProcessor> markdownProcessors)
    {
        markdownProcessors.Add(new LinkToChapters());
        markdownProcessors.Add(new PageBreak());
        markdownProcessors.Add(new LinkToDocuments(file.SourceBaseDirectory, file.SourceAbsolutePath));
        markdownProcessors.Add(new LinkedDocumentsPlaceholder()); // add after the "LinkToDocuments" step, because that one fills the context with the linked documents
    }
}
