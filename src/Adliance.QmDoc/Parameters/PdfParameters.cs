using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Parameters;

[Verb("pdf", true, HelpText = "Runs the full Markdown to PDF conversion.")]
public class PdfParameters : CommonConversionParameters
{
    [Option("theme", Required = false, Default = "", HelpText = "The name of the theme that should be used.")] public string Theme { get; set; } = "";
 }