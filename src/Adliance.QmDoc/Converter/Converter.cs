using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Adliance.QmDoc.Extensions;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.HtmlProcessors;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Themes;
using Humanizer;
using Markdig;

namespace Adliance.QmDoc.Converter;

public abstract partial class Converter(TargetExtension targetExtension, CommonConversionParameters parameters, Options.Options options)
{
    /// <summary>
    /// Where the Markdown is read from with --stdin. Replaceable for tests.
    /// </summary>
    internal Func<TextReader> OpenStandardInput { get; set; } = () => new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));

    /// <summary>
    /// Where the resulting file is written to with --stdout. Replaceable for tests.
    /// </summary>
    internal Func<Stream> OpenStandardOutput { get; set; } = Console.OpenStandardOutput;

    public async Task Run()
    {
        var files = parameters.Stdin
            ? [BuildStdinFile(parameters.Source, parameters.Target, targetExtension)]
            : BuildFilesList(parameters.Source, parameters.Target, targetExtension, parameters.IncludeSubdirectories);
        if (files.Count <= 0)
        {
            var fileInfo = new FileInfo(parameters.Source);
            Program.Exit(-3, $"No files found in {fileInfo.FullName}.");
        }

        if (parameters.Stdout && files.Count > 1)
        {
            throw new InvalidOperationException($"--stdout only works for a single file, but {files.Count} files were found in {parameters.Source}. Please specify a single file with --source.");
        }

        // a broken file (eg. an invalid frontmatter) doesn't stop the conversion of the other files
        var failed = new List<string>();
        foreach (var f in files)
        {
            try
            {
                await ConvertFile(f);
            }
            catch (Exception ex) when (files.Count > 1)
            {
                Program.WriteLine($"\t\t Unable to convert {f.SourceRelativePath}: {Program.Describe(ex)}");
                failed.Add(f.SourceRelativePath);
            }
        }

        if (failed.Count > 0) throw new InvalidOperationException($"{failed.Count} of {files.Count} files could not be converted: {string.Join(", ", failed)}");
    }

    private async Task ConvertFile(ConverterFile f)
    {
        Program.WriteLine(f.SourceRelativePath + " ...");
        var markdownContext = parameters.Stdin ? InitFromStandardInput() : InitFromSourceFile(f);
        markdownContext = RunMarkdownProcessors(f, markdownContext);
        Program.WriteLine($"\tMarkdown ({markdownContext.Markdown.Length.Bytes().Humanize(CultureInfo.CurrentCulture)})");

        if (!parameters.Stdout) EnsureTargetDirectory(f);

        var resultingBytes = await Convert(f, markdownContext);
        var destination = parameters.Stdout ? "stdout" : f.TargetAbsolutePath;
        if (targetExtension == TargetExtension.Pdf)
        {
            Program.WriteLine($"\tPDF (Theme: {markdownContext.Theme}, {resultingBytes.Length.Bytes().Humanize(CultureInfo.CurrentCulture)}) -> {destination}");
        }
        else
        {
            Program.WriteLine($"\t{targetExtension.ToString().ToUpper()} ({resultingBytes.Length.Bytes().Humanize(CultureInfo.CurrentCulture)}) -> {destination}");
        }

        if (parameters.Stdout)
        {
            var stdout = OpenStandardOutput();
            await stdout.WriteAsync(resultingBytes);
            await stdout.FlushAsync();
        }
        else
        {
            await File.WriteAllBytesAsync(f.TargetAbsolutePath, resultingBytes);
        }
    }

    protected abstract Task<byte[]> Convert(ConverterFile file, MarkdownProcessorContext markdownContext);

    private static MarkdownProcessorContext InitFromSourceFile(ConverterFile file)
    {
        return new MarkdownProcessorContext(File.ReadAllText(file.SourceAbsolutePath).Replace("\r\n", "\n"));
    }

    private MarkdownProcessorContext InitFromStandardInput()
    {
        using var reader = OpenStandardInput();
        return new MarkdownProcessorContext(reader.ReadToEnd().Replace("\r\n", "\n"));
    }

    /// <summary>
    /// With --stdin, the source is the path of the Markdown file the text belongs to (eg. an unsaved file in an editor).
    /// It's only used to resolve relative paths, so it doesn't need to exist.
    /// </summary>
    private static ConverterFile BuildStdinFile(string source, string targetBaseDirectory, TargetExtension targetExtension)
    {
        if (Directory.Exists(source) || source.EndsWith('/') || source.EndsWith('\\'))
        {
            throw new InvalidOperationException($"--stdin requires --source to be the path of a Markdown file (it's used to resolve includes, images and links), but \"{source}\" is a directory.");
        }

        var fileInfo = new FileInfo(source);
        return new ConverterFile(fileInfo.DirectoryName!, fileInfo.Name, fileInfo.FullName, targetBaseDirectory, targetExtension);
    }

    protected MarkdownProcessorContext RunMarkdownProcessors(ConverterFile file, MarkdownProcessorContext markdownContext, bool reportErrors = true)
    {
        markdownContext = new IncludesProcessor(file.SourceAbsolutePath, GetTheme(markdownContext)).Apply(markdownContext);
        markdownContext = ApplyCommonPlaceholders(file, markdownContext);
        var markdownProcessors = new List<IMarkdownProcessor>();
        PrepareAdditionalProcessors(file, markdownProcessors);

        foreach (var p in markdownProcessors) markdownContext = p.Apply(markdownContext);
        if (reportErrors) ReportErrors(markdownContext.Errors);
        return markdownContext;
    }

    protected string RunHtmlProcessors(ConverterFile file, MarkdownProcessorContext markdownContext, bool reportErrors = true)
    {
        var html = parameters is HtmlParameters { SourceLines: true }
            ? SourceLines.ToHtml(markdownContext)
            : Markdown.ToHtml(markdownContext.Markdown, markdownContext.Pipeline);

        var theme = GetTheme(markdownContext);
        var layout = ApplyCommonPlaceholders(file, ThemeProvider.GetContent(theme), markdownContext);
        if (markdownContext.Frontmatter.EnableDocumentTitle == false)
        {
            layout = Regex.Replace(layout, "<div class=\"document-title\">.*?</div>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

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
            if (reportErrors) ReportErrors(stepResult.Errors);
            result = stepResult.ResultingHtml;
        }

        result = new BodyPlaceholder(result).Apply(layout).ResultingHtml;
        result = LinkToConvertedDocumentRegex().Replace(result, " href=\"$1.pdf\"");
        result = new MermaidJsSnippet(targetExtension).Apply(result).ResultingHtml;
        return result;
    }

    private static void ReportErrors(IEnumerable<ProcessorError> errors)
    {
        foreach (var e in errors) Program.WriteLine($"\t\t {e.ErrorMessage}");
    }

    /// <summary>
    /// Relative links to other documents ("other.html", see LinkToDocuments) always point to the PDF.
    /// Links to web pages ("https://example.com/page.html") are left alone.
    /// </summary>
    [GeneratedRegex(@" href=""(?![a-z][a-z0-9+.\-]*:|//)([^""]*?)\.html""", RegexOptions.IgnoreCase)]
    private static partial Regex LinkToConvertedDocumentRegex();

    /// <summary>
    /// The Git history of each file, it's needed for the document, the header and the footer (and again for the second pass of the PDF).
    /// </summary>
    private readonly Dictionary<string, List<GitService.Change>> _gitChanges = new();

    private MarkdownProcessorContext ApplyCommonPlaceholders(ConverterFile file, MarkdownProcessorContext context)
    {
        var gitChanges = new List<GitService.Change>();

        // loading the Git history can be slow on large repositories, only load when really necessary;
        // the current Markdown contains included files, or is a template of the theme (eg. the footer with the Git version)
        if (context.ContainsPlaceholder(GitDateAndVersionPlaceholder.Placeholder, GitDatePlaceholder.Placeholder, GitVersionPlaceholder.Placeholder, GitVersionsPlaceholder.Placeholder))
        {
            if (!_gitChanges.TryGetValue(file.SourceAbsolutePath, out var cached))
            {
                cached = GitService.GetVersions(
                    file.SourceAbsolutePath,
                    parameters.IgnoreGitCommitsSince,
                    parameters.IgnoreGitCommits.SplitCleanOrder(),
                    parameters.IgnoreGitCommitsWithout.SplitCleanOrder()).ToList();
                _gitChanges[file.SourceAbsolutePath] = cached;
            }

            gitChanges = cached;
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
            new FrontmatterValuesPlaceholder(),
            new CalculationPlaceholders(file.SourceAbsolutePath), // after the frontmatter values, so that "{{ = 2 * {{ Faktor }} }}" works
            // after all placeholders that may change the text of headings, so that the TOC links to the final ids of the headings
            new HeaderNumbering(context.Frontmatter.EnableHeaderNumbering != false),
            new TableOfContentsPlaceholder()
        };

        foreach (var p in processors) context = p.Apply(context);
        return context;
    }

    protected string ApplyCommonPlaceholders(ConverterFile file, string applyPlaceholdersTo, MarkdownProcessorContext markdownContext)
    {
        var previousMarkdown = markdownContext.Markdown;
        markdownContext.Markdown = applyPlaceholdersTo;
        markdownContext.IsTemplate = true;
        try
        {
            return ApplyCommonPlaceholders(file, markdownContext).Markdown;
        }
        finally
        {
            markdownContext.Markdown = previousMarkdown;
            markdownContext.IsTemplate = false;
        }
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
        else if (parameters is ThemedConversionParameters themedParameters && !string.IsNullOrWhiteSpace(themedParameters.Theme)) context.Theme = themedParameters.Theme;

        if (string.IsNullOrWhiteSpace(context.Theme)) context.Theme = ThemeProvider.DefaultTheme;
        if (!ThemeProvider.Exists(context.Theme))
        {
            Program.WriteLine($"\t\t Unknown theme \"{context.Theme}\", using the default theme \"{ThemeProvider.DefaultTheme}\" instead. Available themes: {string.Join(", ", ThemeProvider.AvailableThemes())}.");
            context.Theme = ThemeProvider.DefaultTheme;
        }

        return context.Theme;
    }

    private static void EnsureTargetDirectory(ConverterFile file)
    {
        var targetDirectory = new DirectoryInfo(Path.GetDirectoryName(file.TargetAbsolutePath)!);
        if (!targetDirectory.Exists) targetDirectory.Create();
    }

    /// <summary>
    /// Adds the processors that run after the common placeholders, by default the ones for links and page breaks of the HTML/PDF.
    /// </summary>
    protected virtual void PrepareAdditionalProcessors(ConverterFile file, IList<IMarkdownProcessor> markdownProcessors)
    {
        markdownProcessors.Add(new LinkToChapters(file.SourceAbsolutePath));
        markdownProcessors.Add(new PageBreak());
        markdownProcessors.Add(new LinkToDocuments(file.SourceBaseDirectory, file.SourceAbsolutePath));
        markdownProcessors.Add(new LinkedDocumentsPlaceholder()); // add after the "LinkToDocuments" step, because that one fills the context with the linked documents
    }

    private static IList<ConverterFile> BuildFilesList(string source, string targetBaseDirectory, TargetExtension targetExtension, bool includeSubdirectories)
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
            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var fileInfo in baseDirectory.GetFiles("*.md", searchOption).OrderBy(x => x.FullName))
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
    Docx,
    Html
}

public class ConverterFile
{
    public ConverterFile(string sourceBaseDirectory, string sourceRelativePath, string sourceAbsolutePath, string targetBaseDirectory, TargetExtension targetExtension)
    {
        SourceBaseDirectory = sourceBaseDirectory;
        SourceRelativePath = sourceRelativePath;
        SourceAbsolutePath = sourceAbsolutePath;
        TargetAbsolutePath = Path.Combine(targetBaseDirectory, SourceRelativePath);
        TargetAbsolutePath = new FileInfo(TargetAbsolutePath[..^Path.GetExtension(SourceRelativePath).Length] + "." + targetExtension.ToString().ToLower()).FullName;
    }

    public string SourceBaseDirectory { get; }
    public string SourceRelativePath { get; }
    public string SourceAbsolutePath { get; }
    public string TargetAbsolutePath { get; }
}
