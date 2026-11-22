using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Parameters;

[Verb("html", false, HelpText = "Runs the full Markdown to HTML conversion.")]
public class HtmlParameters : ThemedConversionParameters
{
    [Option("source-lines", Required = false, Default = false, HelpText = "Adds \"data-line\" attributes with the (zero-based) line in the source file to the HTML elements, eg. for scrolling editor previews to the same position as the editor.")] public bool SourceLines { get; set; } = false;
}
