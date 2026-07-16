using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Parameters;

[Verb("html", false, HelpText = "Runs the full Markdown to HTML conversion.")]
public class HtmlParameters : ThemedConversionParameters;
