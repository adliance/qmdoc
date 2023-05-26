using CommandLine;
using CommandLine.Text;
using System;
using System.Threading.Tasks;
using Adliance.QmDoc.Converter;
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
                    var options = OptionsProvider.LoadOptions();
                    new PdfConverter(p, options).Run();
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
                    var options = OptionsProvider.LoadOptions();
                    new DocxConverter(p, options).Run();
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
                    h.AdditionalNewLineAfterOption = false;
                    return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                }, e => e);
                Console.Error.Write(helpText);
                Exit(-1);
            });
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

    private static async Task SetTheme(SetThemeParameters parameters)
    {
        var options = OptionsProvider.LoadOptions();
        await UpdateTheme(options, parameters.Theme);
        Console.WriteLine($"Changed theme to \"{parameters.Theme}\". All conversions will use this theme from now on.");
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
}