using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Parameters;

[Verb("pdf-and-html", false, HelpText = "Runs both the Markdown to PDF and the Markdown to HTML conversion.")]
public class PdfAndHtmlParameters : ThemedConversionParameters;
