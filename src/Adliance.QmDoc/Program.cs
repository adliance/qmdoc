using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Adliance.QmDoc.BeforeConversionToPdf;
using Adliance.QmDoc.Configuration;
using Adliance.QmDoc.Themes;

namespace Adliance.QmDoc
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        private static void Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<RunParameters, SetThemeParameters>(args);
            parserResult
                .WithParsed<RunParameters>(p =>
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
            var options = AppOptionsProvider.LoadOptions();
            await UpdateTheme(options, parameters.Theme);
            Console.WriteLine($"Changed theme to \"{parameters.Theme}\". All conversions will use this theme from now on.");
        }

        private static async Task Run(RunParameters parameters)
        {
            var options = AppOptionsProvider.LoadOptions();

            await UpdateTheme(options, parameters.Theme);

            if (File.Exists(parameters.Source))
            {
                await Convert(parameters.Source, options, parameters);
            }
            else if (Directory.Exists(parameters.Source))
            {
                foreach (var file in Directory.GetFiles(parameters.Source, "*.md").OrderBy(x => x))
                {
                    try
                    {
                        await Convert(file, options, parameters);
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

        private static async Task UpdateTheme(AppOptions options, string theme)
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
            AppOptionsProvider.StoreOptions(options);
        }

        private static async Task Convert(string sourceFilePath, AppOptions options, RunParameters parameters)
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
                targetDirectory = Path.GetDirectoryName(sourceFilePath) ?? "";
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
                html = MarkdownToHtmlConverter.ConvertMarkdownToHtml(theme, sourceFilePath, title, parameters.DisableHeaderNumbering, out var e);
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
                    var targetHtmlPath = Path.Combine(targetDirectory, Path.GetFileName(sourceFilePath).Replace(".md", ".html"));
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
                    var targetPdfPath = Path.Combine(targetDirectory, Path.GetFileName(sourceFilePath).Replace(".md", ".pdf"));
                    await HtmlToPdfConverter.ConvertHtmlTPdf(theme, html, targetPdfPath, title);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to create PDF file: {ex.Message}", ex);
                }
            }

            if (errors.Any())
            {
                Console.WriteLine("The following problems have been found:");
                foreach (var e in errors.OrderBy(x => x.FilePath))
                {
                    Console.WriteLine($"  - {(e.IsWarningOnly ? " WARN " : "ERROR")}\t{e.FileName}\t {e.ErrorMessage} ");
                }
            }
        }
    }
}