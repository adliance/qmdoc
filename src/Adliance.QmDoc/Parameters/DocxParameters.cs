using System;
using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Parameters;

[Verb("docx", false, HelpText = "Runs the Markdown to DOCX conversion. Only supports a subset of features compared to the PDF conversion.")]
public class DocxParameters : CommonConversionParameters
{
}