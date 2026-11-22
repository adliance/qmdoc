using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Adliance.QmDoc.Converter;
using Adliance.QmDoc.Converter.Docx;
using Adliance.QmDoc.Options;
using Adliance.QmDoc.Parameters;
using Adliance.QmDoc.Themes;
using CommandLine;
using CommandLine.Text;

namespace Adliance.QmDoc;

// ReSharper disable once ClassNeverInstantiated.Global
public class Program
{
    private static void Main(string[] args)
    {
        var parserResult = Parser.Default.ParseArguments<PdfParameters, HtmlParameters, PdfAndHtmlParameters, PdfAndHtmlAndMarkdownParameters, DocxParameters, SetThemeParameters, UpdateParameters>(args);
        parserResult
            .WithParsed<PdfParameters>(p =>
            {
                try
                {
                    PrepareStandardStreams(p);
                    var options = OptionsProvider.LoadOptions();
                    new PdfConverter(p, options).Run().GetAwaiter().GetResult();
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-2, Describe(ex));
                }
            })
            .WithParsed<HtmlParameters>(p =>
            {
                try
                {
                    PrepareStandardStreams(p);
                    var options = OptionsProvider.LoadOptions();
                    new HtmlConverter(p, options).Run().GetAwaiter().GetResult();
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-2, Describe(ex));
                }
            })
            .WithParsed<PdfAndHtmlParameters>(p =>
            {
                try
                {
                    if (p.Stdout) throw new InvalidOperationException("--stdout only works for commands that create a single file (pdf, html, docx).");
                    var options = OptionsProvider.LoadOptions();
                    var htmlConverter = new HtmlConverter(p, options);
                    var pdfConverter = new PdfConverter(p, options);
                    ShareStandardInput(p, htmlConverter, pdfConverter);
                    htmlConverter.Run().GetAwaiter().GetResult();
                    pdfConverter.Run().GetAwaiter().GetResult();
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-2, Describe(ex));
                }
            })
            .WithParsed<PdfAndHtmlAndMarkdownParameters>(p =>
            {
                try
                {
                    if (p.Stdout) throw new InvalidOperationException("--stdout only works for commands that create a single file (pdf, html, docx).");
                    var options = OptionsProvider.LoadOptions();
                    var htmlConverter = new HtmlConverter(p, options, saveMarkdown: true);
                    var pdfConverter = new PdfConverter(p, options);
                    ShareStandardInput(p, htmlConverter, pdfConverter);
                    htmlConverter.Run().GetAwaiter().GetResult();
                    pdfConverter.Run().GetAwaiter().GetResult();
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-2, Describe(ex));
                }
            })
            .WithParsed<DocxParameters>(p =>
            {
                try
                {
                    PrepareStandardStreams(p);
                    var options = OptionsProvider.LoadOptions();
                    new DocxConverter(p, options).Run().GetAwaiter().GetResult();
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-2, Describe(ex));
                }
            })
            .WithParsed<SetThemeParameters>(p =>
            {
                try
                {
                    SetTheme(p);
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-3, Describe(ex));
                }
            })
            .WithParsed<UpdateParameters>(p =>
            {
                try
                {
                    new UpdateService().Run().GetAwaiter().GetResult();
                    Exit(0);
                }
                catch (Exception ex)
                {
                    Exit(-3, Describe(ex));
                }
            })
            .WithNotParsed(errs =>
            {
                var helpText = HelpText.AutoBuild(parserResult, h =>
                {
                    h.AdditionalNewLineAfterOption = false;
                    return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                }, e => e);
                Console.Error.Write(helpText);
                Exit(-1);
            });
    }

    /// <summary>
    /// With --stdout, standard output only contains the resulting file, so all log messages go to standard error.
    /// </summary>
    private static void PrepareStandardStreams(CommonConversionParameters parameters)
    {
        if (parameters.Stdout) Console.SetOut(Console.Error);
    }

    /// <summary>
    /// Standard input can only be read once, but commands that create several files run a converter for each of them.
    /// </summary>
    private static void ShareStandardInput(CommonConversionParameters parameters, params Converter.Converter[] converters)
    {
        if (!parameters.Stdin) return;
        string markdown;
        using (var reader = converters[0].OpenStandardInput()) markdown = reader.ReadToEnd();
        foreach (var converter in converters) converter.OpenStandardInput = () => new StringReader(markdown);
    }

    /// <summary>
    /// Includes the inner exceptions, eg. "Exception during deserialization" alone doesn't say what's wrong with the frontmatter.
    /// </summary>
    public static string Describe(Exception ex)
    {
        var messages = new List<string>();
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (!messages.Contains(e.Message)) messages.Add(e.Message);
        }

        return string.Join(" ", messages);
    }

    public static void Exit(int code, string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message)) WriteLine(message);
        Environment.Exit(code);
    }

    public static void WriteLine(string? message)
    {
        Console.WriteLine(message);
    }

    public static void Write(string? message)
    {
        Console.Write(message);
    }

    private static void SetTheme(SetThemeParameters parameters)
    {
        var options = OptionsProvider.LoadOptions();
        UpdateTheme(options, parameters.Theme);
        Console.WriteLine($"Changed theme to \"{parameters.Theme}\". All conversions will use this theme from now on.");
    }

    private static void UpdateTheme(Options.Options options, string theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
        {
            return;
        }

        if (!ThemeProvider.Exists(theme))
        {
            throw new InvalidOperationException($"Unknown theme \"{theme}\". Available themes: {string.Join(", ", ThemeProvider.AvailableThemes())}.");
        }

        options.Theme = theme;
        OptionsProvider.StoreOptions(options);
    }
}
