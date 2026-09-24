using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Parameters;

[Verb("pdf-html-markdown", false, HelpText = "Runs both the Markdown to PDF and the Markdown to HTML conversion, and additionally stores the processed Markdown that is used to generate the HTML.")]
public class PdfAndHtmlAndMarkdownParameters : ThemedConversionParameters;
