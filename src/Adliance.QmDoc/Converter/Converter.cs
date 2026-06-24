using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Adliance.QmDoc.Extensions;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.HtmlProcessors;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Themes;
using Humanizer;
using Markdig;
using TitlePlaceholder = Adliance.QmDoc.Processors.MarkdownProcessors.TitlePlaceholder;

namespace Adliance.QmDoc.Converter;

public abstract class Converter(TargetExtension targetExtension, CommonConversionParameters parameters, Options.Options options)
{
    public async Task Run()
    {
        var files = BuildFilesList(parameters.Source, parameters.Target, targetExtension);
        if (files.Count <= 0) Program.Exit(-3, $"No files found in {parameters.Source}.");

        foreach (var f in files)
        {
            Program.WriteLine(f.SourceRelativePath + " ...");
            var markdownContext = InitFromSourceFile(f);
            markdownContext = RunMarkdownProcessors(f, markdownContext);
            Program.WriteLine($"\tMarkdown ({markdownContext.Markdown.Length.Bytes().Humanize(CultureInfo.CurrentCulture)})");

            EnsureTargetDirectory(f);

            if (parameters.IncludeHtml)
            {
                var html = RunHtmlProcessors(f, markdownContext);
                var targetPathForHtmlFile = f.TargetAbsolutePath[..^Path.GetExtension(f.TargetAbsolutePath).Length] + ".html";
                Program.WriteLine($"\tHTML (Theme: {markdownContext.Theme}, {html.Length.Bytes().Humanize(CultureInfo.CurrentCulture)}) -> {targetPathForHtmlFile}");
                await File.WriteAllTextAsync(targetPathForHtmlFile, html);
            }

            var resultingBytes = await Convert(f, markdownContext);
            if (targetExtension == TargetExtension.Pdf)
            {
                Program.WriteLine($"\tPDF (Theme: {markdownContext.Theme}, {resultingBytes.Length.Bytes().Humanize(CultureInfo.CurrentCulture)}) -> {f.TargetAbsolutePath}");
            }
            else
            {
                Program.WriteLine($"\t{targetExtension.ToString().ToUpper()} ({resultingBytes.Length.Bytes().Humanize(CultureInfo.CurrentCulture)}) -> {f.TargetAbsolutePath}");
            }

            await File.WriteAllBytesAsync(f.TargetAbsolutePath, resultingBytes);
        }
    }

    protected abstract Task<byte[]> Convert(ConverterFile file, MarkdownProcessorContext markdownContext);

    private static MarkdownProcessorContext InitFromSourceFile(ConverterFile file)
    {
        var sourceMarkdown = File.ReadAllText(file.SourceAbsolutePath).Replace("\r\n", "\n");
        var markdownContext = new MarkdownProcessorContext(sourceMarkdown);
        return markdownContext;
    }

    protected MarkdownProcessorContext RunMarkdownProcessors(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        markdownContext = ApplyCommonPlaceholders(file, markdownContext);
        var markdownProcessors = new List<IMarkdownProcessor>
        {
            new ImagesMustNotContainSpaces(file.SourceAbsolutePath)
        };
        PrepareAdditionalProcessors(file, markdownProcessors);

        foreach (var p in markdownProcessors) markdownContext = p.Apply(markdownContext);
        foreach (var e in markdownContext.Errors) Program.WriteLine($"\t\t {e.ErrorMessage}");
        return markdownContext;
    }

    protected string RunHtmlProcessors(ConverterFile file, MarkdownProcessorContext markdownContext)
    {
        var html = Markdown.ToHtml(markdownContext.Markdown, markdownContext.Pipeline);

        var theme = GetTheme(markdownContext);
        var layout = ApplyCommonPlaceholders(file, ThemeProvider.GetContent(theme), markdownContext);

        IHtmlProcessor[] steps =
        [
            new AuthorLine(markdownContext.Frontmatter.Author), // should be the first step
            new IconBlocks(),
            new IconLists(),
            new SetCorrectChaptersLinkTitle(file.SourceAbsolutePath),
            new EmbedImages(file.SourceAbsolutePath),
            new ConfigurablePlaceholders(file.SourceAbsolutePath, parameters.PlaceholdersFile)
        ];

        var result = html;
        foreach (var step in steps)
        {
            var stepResult = step.Apply(result);
            foreach (var e in stepResult.Errors) Program.WriteLine($"\t\t {e.ErrorMessage}");
            result = stepResult.ResultingHtml;
        }

        result = new BodyPlaceholder(result).Apply(layout).ResultingHtml;
        result = Regex.Replace(result, " href=\"(.*?)\\.html\"", " href=\"$1.pdf\"", RegexOptions.IgnoreCase);
        return result;
    }

    private MarkdownProcessorContext ApplyCommonPlaceholders(ConverterFile file, MarkdownProcessorContext context)
    {
        var gitChanges = new List<GitService.Change>();

        // loading the Git history can be slow on large repositories, only load when really necessary
        if (context.ContainsPlaceholderInSource(GitDateAndVersionPlaceholder.Placeholder, GitDatePlaceholder.Placeholder, GitVersionPlaceholder.Placeholder, GitVersionsPlaceholder.Placeholder)) {
            Program.WriteLine("\tLoading git versions ...");
            gitChanges = GitService.GetVersions(
                file.SourceAbsolutePath,
                parameters.IgnoreGitCommitsSince,
                parameters.IgnoreGitCommits.SplitCleanOrder(),
                parameters.IgnoreGitCommitsWithout.SplitCleanOrder()).ToList();
        }

        var gitLatestChange = gitChanges.FirstOrDefault();

        var processors = new List<IMarkdownProcessor>
        {
            new TitlePlaceholder(GetTitle(file, context)),
            new DatePlaceholder(),
            new GitVersionsPlaceholder(gitChanges),
            new GitVersionPlaceholder(gitLatestChange),
            new GitDatePlaceholder(gitLatestChange),
            new GitDateAndVersionPlaceholder(gitLatestChange),
            new CssPlaceholder(GetTheme(context)),
            new HeaderNumbering(!parameters.DisableHeaderNumbering),
            new TableOfContentsPlaceholder()
        };

        foreach (var p in processors) context = p.Apply(context);
        return context;
    }

    protected string ApplyCommonPlaceholders(ConverterFile file, string applyPlaceholdersTo, MarkdownProcessorContext markdownContext)
    {
        var previousMarkdown = markdownContext.Markdown;
        markdownContext.Markdown = applyPlaceholdersTo;
        var result = ApplyCommonPlaceholders(file, markdownContext).Markdown;
        markdownContext.Markdown = previousMarkdown;
        return result;
    }

    private string GetTitle(ConverterFile file, MarkdownProcessorContext context)
    {
        var result = Path.GetFileNameWithoutExtension(file.SourceAbsolutePath);
        if (!string.IsNullOrWhiteSpace(context.Frontmatter.Title)) result = context.Frontmatter.Title;
        if (!string.IsNullOrWhiteSpace(parameters.Title)) result = parameters.Title;
        return result;
    }

    protected string GetTheme(MarkdownProcessorContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Theme)) return context.Theme;

        context.Theme = options.Theme;
        if (!string.IsNullOrWhiteSpace(context.Frontmatter.Theme)) context.Theme = context.Frontmatter.Theme;
        else if (parameters is PdfParameters pdfParameters && !string.IsNullOrWhiteSpace(pdfParameters.Theme)) context.Theme = pdfParameters.Theme;

        if (string.IsNullOrWhiteSpace(context.Theme)) context.Theme = "Default";
        return context.Theme;
    }

    private static void EnsureTargetDirectory(ConverterFile file)
    {
        var targetDirectory = new DirectoryInfo(Path.GetDirectoryName(file.TargetAbsolutePath)!);
        if (!targetDirectory.Exists) targetDirectory.Create();
    }

    protected abstract void PrepareAdditionalProcessors(ConverterFile file, IList<IMarkdownProcessor> markdownProcessors);

    private static IList<ConverterFile> BuildFilesList(string source, string targetBaseDirectory, TargetExtension targetExtension)
    {
        var result = new List<ConverterFile>();

        if (File.Exists(source))
        {
            var fileInfo = new FileInfo(source);
            result.Add(new ConverterFile(fileInfo.DirectoryName!, fileInfo.Name, fileInfo.FullName, targetBaseDirectory, targetExtension));
        }
        else if (Directory.Exists(source))
        {
            var baseDirectory = new DirectoryInfo(source.TrimEnd('/', '\\'));
            foreach (var fileInfo in baseDirectory.GetFiles("*.md", SearchOption.AllDirectories).OrderBy(x => x.FullName))
            {
                result.Add(new ConverterFile(baseDirectory.FullName, fileInfo.FullName[(baseDirectory.FullName.Length + 1)..], fileInfo.FullName, targetBaseDirectory, targetExtension));
            }
        }

        return result;
    }
}

public enum TargetExtension
{
    Pdf,
    Docx
}

public class ConverterFile
{
    public ConverterFile(string sourceBaseDirectory, string sourceRelativePath, string sourceAbsolutePath, string targetBaseDirectory, TargetExtension targetExtension)
    {
        SourceBaseDirectory = sourceBaseDirectory;
        SourceRelativePath = sourceRelativePath;
        SourceAbsolutePath = sourceAbsolutePath;
        TargetAbsolutePath = Path.Combine(targetBaseDirectory, SourceRelativePath);
        TargetAbsolutePath = TargetAbsolutePath[..^Path.GetExtension(SourceRelativePath).Length] + "." + targetExtension.ToString().ToLower();
    }

    public string SourceBaseDirectory { get; }
    public string SourceRelativePath { get; }
    public string SourceAbsolutePath { get; }
    public string TargetAbsolutePath { get; }
}
