using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Adliance.QmDoc.BeforeConversionToPdf;
using Adliance.QmDoc.Extensions;
using Adliance.QmDoc.Options;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc;

// ReSharper disable once ClassNeverInstantiated.Global
public class Program
{
    private static void Main(string[] args)
    {
        var parserResult = Parser.Default.ParseArguments<PdfParameters, DocxParameters, SetThemeParameters, UpdateParameters>(args);
        parserResult
            .WithParsed<PdfParameters>(p =>
            {
                try
                {
                    Run(p).GetAwaiter().GetResult();
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-2, ex.Message);
                }
            })
            .WithParsed<DocxParameters>(p =>
            {
                try
                {
                    
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-2, ex.Message);
                }
            })
            .WithParsed<SetThemeParameters>(p =>
            {
                try
                {
                    SetTheme(p).GetAwaiter().GetResult();
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-3, ex.Message);
                }
            })
            .WithParsed<UpdateParameters>(p =>
            {
                try
                {
                    new UpdateService().Run();
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-3, ex.Message);
                }
            })
            .WithNotParsed(errs =>
            {
                var helpText = HelpText.AutoBuild(parserResult, h =>
                {
                    // Configure HelpText here  or create your own and return it 
                    h.AdditionalNewLineAfterOption = false;
                    return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                }, e => e);
                Console.Error.Write(helpText);
                Exit(-1);
            });
    }

    private static void Exit(int code, string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine(message);
        }

        Environment.Exit(code);
    }

    private static async Task SetTheme(SetThemeParameters parameters)
    {
        var options = OptionsProvider.LoadOptions();
        await UpdateTheme(options, parameters.Theme);
        Console.WriteLine($"Changed theme to \"{parameters.Theme}\". All conversions will use this theme from now on.");
    }

    private static async Task Run(PdfParameters parameters)
    {
        var options = OptionsProvider.LoadOptions();

        await UpdateTheme(options, parameters.Theme);

        if (File.Exists(parameters.Source))
        {
            await Convert(new FileInfo(parameters.Source).DirectoryName!, parameters.Source, options, parameters);
        }
        else if (Directory.Exists(parameters.Source))
        {
            foreach (var file in Directory.GetFiles(parameters.Source, "*.md", SearchOption.AllDirectories).OrderBy(x => x))
            {
                try
                {
                    await Convert(new DirectoryInfo(parameters.Source).FullName!, file, options, parameters);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to convert {file}: {ex.Message}");
                }
            }
        }
        else
        {
            Exit(-3, $"Source file or directory '{parameters.Source}' does not exist.");
        }
    }

    private static async Task UpdateTheme(Options.Options options, string theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
        {
            return;
        }

        if (!options.Theme.Equals(theme, StringComparison.OrdinalIgnoreCase) || options.ThemeRefreshedUtc < DateTime.UtcNow.AddMinutes(-5))
        {
            await ThemeFetcher.Fetch(theme);
            options.ThemeRefreshedUtc = DateTime.UtcNow;
        }

        options.Theme = theme;
        OptionsProvider.StoreOptions(options);
    }

    private static async Task Convert(string baseDirectory, string sourceFilePath, Options.Options options, PdfParameters parameters)
    {
        var theme = !string.IsNullOrWhiteSpace(parameters.Theme) ? parameters.Theme : options.Theme;

        if (!File.Exists(sourceFilePath))
        {
            Console.WriteLine($"Source file '{sourceFilePath}' does not exist.");
            return;
        }

        if (!Path.GetExtension(sourceFilePath).Equals(".md", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Source file '{sourceFilePath}' is not a markdown file.");
            return;
        }

        var targetDirectory = parameters.Target;
        if (targetDirectory.Equals("./"))
        {
            targetDirectory = Path.GetDirectoryName(baseDirectory) ?? "";
        }

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var title = string.IsNullOrEmpty(parameters.Title) ? Path.GetFileNameWithoutExtension(sourceFilePath) : parameters.Title;

        if (string.IsNullOrWhiteSpace(theme))
        {
            Console.WriteLine($"Working on \"{sourceFilePath}\" (no theme) ...");
        }
        else
        {
            Console.WriteLine($"Working on \"{sourceFilePath}\" (theme \"{theme}\") ...");
        }

        var errors = new List<ProcessorError>();
        string html;
        try
        {
            html = MarkdownToHtmlConverter.ConvertMarkdownToHtml(
                theme,
                baseDirectory,
                sourceFilePath,
                title,
                parameters.DisableHeaderNumbering,
                parameters.IgnoreGitCommitsSince,
                parameters.IgnoreGitCommits.SplitCleanOrder(),
                parameters.IgnoreGitCommitsWithout.SplitCleanOrder(),
                parameters.PlaceholdersFile ?? "",
                out var e);
            errors.AddRange(e);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to convert markdown file: {ex.Message}", ex);
        }

        if (parameters.IncludeHtml)
        {
            try
            {
                var relativeTargetHtmlPath = Path.GetRelativePath(baseDirectory, sourceFilePath).Replace(".md", ".html");
                var targetHtmlPath = Path.Combine(targetDirectory, relativeTargetHtmlPath);
                var targetHtmlDirectory = Path.GetDirectoryName(targetHtmlPath);
                if (!Directory.Exists(targetHtmlDirectory))
                {
                    Directory.CreateDirectory(targetHtmlDirectory!);
                }

                await File.WriteAllTextAsync(targetHtmlPath, html);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to save HTML file: {ex.Message}", ex);
            }
        }

        var beforePdfSteps = new IBeforeConversionToPdfStep[]
        {
            new ChangeLinkToDocumentsFromHtmlToPdf(sourceFilePath)
        };
        foreach (var s in beforePdfSteps)
        {
            var result = s.Apply(html);
            html = result.ResultingHtml;
            errors.AddRange(result.Errors);
        }

        if (!parameters.ExcludePdf)
        {
            try
            {
                var relativeTargetPdfPath = Path.GetRelativePath(baseDirectory, sourceFilePath).Replace(".md", ".pdf");
                var targetPdfPath = Path.Combine(targetDirectory, relativeTargetPdfPath);
                var targetPdfDirectory = Path.GetDirectoryName(targetPdfPath);
                if (!Directory.Exists(targetPdfDirectory))
                {
                    Directory.CreateDirectory(targetPdfDirectory!);
                }

                await HtmlToPdfConverter.ConvertHtmlTPdf(
                    theme,
                    html,
                    baseDirectory,
                    sourceFilePath,
                    targetPdfPath,
                    title,
                    parameters.IgnoreGitCommitsSince,
                    parameters.IgnoreGitCommits.SplitCleanOrder(),
                    parameters.IgnoreGitCommitsWithout.SplitCleanOrder());
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to create PDF file: {ex.Message}", ex);
            }
        }

        if (errors.Any())
        {
            Console.WriteLine("The following problems have been found:");
            foreach (var e in errors.Distinct().OrderBy(x => x.FilePath).ThenBy(x => x.ErrorMessage))
            {
                Console.WriteLine($"  - {(e.IsWarningOnly ? " WARN " : "ERROR")}\t{e.FileName}\t {e.ErrorMessage} ");
            }
        }
    }
}