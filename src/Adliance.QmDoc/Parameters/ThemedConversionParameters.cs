using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Parameters;

public abstract class ThemedConversionParameters : CommonConversionParameters
{
    [Option("theme", Required = false, Default = "", HelpText = "The name of the theme that should be used.")] public string Theme { get; set; } = "";
}
