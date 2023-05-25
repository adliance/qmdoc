using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Parameters;

[Verb("set-theme", HelpText = "Sets the theme that will be used for all conversions.")]
public class SetThemeParameters
{
    [Option("theme", Required = true, Default = "", HelpText = "The name of the theme that should be used.")] public string Theme { get; set; } = "";
}