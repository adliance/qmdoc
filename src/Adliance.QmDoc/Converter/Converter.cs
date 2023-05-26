using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Adliance.QmDoc.Extensions;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Processors.HtmlProcessors;
using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Themes;
using Markdig;
using TitlePlaceholder = Adliance.QmDoc.Processors.MarkdownProcessors.TitlePlaceholder;

namespace Adliance.QmDoc.Converter;

public abstract class Converter
{
    private readonly TargetExtension _targetExtension;
    private readonly CommonConversionParameters _parameters;
    private readonly Options.Options _options;

    protected Converter(TargetExtension targetExtension, CommonConversionParameters parameters, Options.Options options)
    {
        _targetExtension = targetExtension;
        _parameters = parameters;
        _options = options;
    }

    public void Run()
    {
        var files = BuildFilesList(_parameters.Source, _parameters.Target, _targetExtension);
        if (files.Count <= 0) Program.Exit(-3, $"No files found in {_parameters.Source}.");

        foreach (var f in files)
        {
            Program.WriteLine(f.SourceRelativePath + " ...");
            var markdown = LoadMarkdown(f);
            Program.WriteLine($"\t Markdown ({FormatBytes(markdown.Length)})");

            EnsureTargetDirectory(f);

            if (_parameters.IncludeHtml)
            {
                var html = LoadHtml(f, markdown);
                var targetPathForHtmlFile = f.TargetAbsolutePath[..^Path.GetExtension(f.TargetAbsolutePath).Length] + ".html";
                Program.WriteLine($"\t HTML ({FormatBytes(html.Length)}) -> {targetPathForHtmlFile}");
                File.WriteAllText(targetPathForHtmlFile, html);
            }

            var resultingBytes = Convert(f, markdown);
            Program.WriteLine($"\t {_targetExtension.ToString().ToUpper()} ({FormatBytes(resultingBytes.Length)}) -> {f.TargetAbsolutePath}");
            File.WriteAllBytes(f.TargetAbsolutePath, resultingBytes);
        }
    }

    protected abstract byte[] Convert(ConverterFile file, string markdown);

    private string LoadMarkdown(ConverterFile file)
    {
        var markdown = File.ReadAllText(file.SourceAbsolutePath);

        markdown = ApplyCommonPlaceholders(file, markdown);
        var markdownProcessors = new List<IMarkdownProcessor>
        {
            new ImagesMustNotContainSpaces(file.SourceAbsolutePath)
        };
        PrepareAdditionalProcessors(file, markdownProcessors);

        var context = new MarkdownProcessorContext();
        foreach (var p in markdownProcessors)
        {
            var stepResult = p.Apply(markdown, context);
            foreach (var e in stepResult.Errors) Program.WriteLine($"\t\t {e.ErrorMessage}");
            markdown = stepResult.ResultingMarkdown;
        }

        return markdown;
    }

    protected string LoadHtml(ConverterFile file, string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdown.ToHtml(markdown, pipeline);

        var theme = _options.Theme;
        if (_parameters is PdfParameters pdfParameters && !string.IsNullOrWhiteSpace(pdfParameters.Theme)) theme = pdfParameters.Theme;
        var layout = ApplyCommonPlaceholders(file, ThemeProvider.GetContent(theme));

        IHtmlProcessor[] steps =
        {
            new BodyPlaceholder(html), // should be the first step
            new AuthorLine(),
            new IconBlocks(),
            new IconLists(),
            new SetCorrectChaptersLinkTitle(file.SourceAbsolutePath),
            new EmbedImages(file.SourceAbsolutePath),
            new ConfigurablePlaceholders(file.SourceAbsolutePath, _parameters.PlaceholdersFile)
        };

        var result = layout;
        foreach (var step in steps)
        {
            var stepResult = step.Apply(result);
            foreach (var e in stepResult.Errors) Program.WriteLine($"\t\t {e.ErrorMessage}");
            result = stepResult.ResultingHtml;
        }

        result = Regex.Replace(result, " href=\"(.*?)\\.html\"", " href=\"$1.pdf\"", RegexOptions.IgnoreCase);
        return result;
    }

    protected string ApplyCommonPlaceholders(ConverterFile file, string content)
    {
        var title = string.IsNullOrWhiteSpace(_parameters.Title) ? Path.GetFileNameWithoutExtension(file.SourceAbsolutePath) : _parameters.Title;
        var theme = _options.Theme;
        if (_parameters is PdfParameters pdfParameters && !string.IsNullOrWhiteSpace(pdfParameters.Theme)) theme = pdfParameters.Theme;

        var processors = new List<IMarkdownProcessor>
        {
            new TitlePlaceholder(title),
            new DatePlaceholder(),
            new GitVersionsPlaceholder(file.SourceAbsolutePath, _parameters.IgnoreGitCommitsSince, _parameters.IgnoreGitCommits.SplitCleanOrder(), _parameters.IgnoreGitCommitsWithout.SplitCleanOrder()),
            new GitVersionPlaceholder(file.SourceAbsolutePath, _parameters.IgnoreGitCommitsSince, _parameters.IgnoreGitCommits.SplitCleanOrder(), _parameters.IgnoreGitCommitsWithout.SplitCleanOrder()),
            new GitDatePlaceholder(file.SourceAbsolutePath, _parameters.IgnoreGitCommitsSince, _parameters.IgnoreGitCommits.SplitCleanOrder(), _parameters.IgnoreGitCommitsWithout.SplitCleanOrder()),
            new GitDateAndVersionPlaceholder(file.SourceAbsolutePath, _parameters.IgnoreGitCommitsSince, _parameters.IgnoreGitCommits.SplitCleanOrder(), _parameters.IgnoreGitCommitsWithout.SplitCleanOrder()),
            new CssPlaceholder(theme),
            new HeaderNumbering(!_parameters.DisableHeaderNumbering)
        };

        var context = new MarkdownProcessorContext();
        foreach (var p in processors)
        {
            var stepResult = p.Apply(content, context);
            foreach (var e in stepResult.Errors) Program.WriteLine($"\t\t {e.ErrorMessage}");
            content = stepResult.ResultingMarkdown;
        }

        return content;
    }

    private void EnsureTargetDirectory(ConverterFile file)
    {
        var targetDirectory = new DirectoryInfo(Path.GetDirectoryName(file.TargetAbsolutePath)!);
        if (!targetDirectory.Exists) targetDirectory.Create();
    }

    protected abstract void PrepareAdditionalProcessors(ConverterFile file, IList<IMarkdownProcessor> markdownProcessors);

    private IList<ConverterFile> BuildFilesList(string source, string targetBaseDirectory, TargetExtension targetExtension)
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

    private string FormatBytes(int bytes)
    {
        return Math.Ceiling(bytes / 1024d).ToString("N0") + " kB";
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