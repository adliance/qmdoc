using CommandLine;

namespace Adliance.QmDoc.Parameters;

public abstract class CommonConversionParameters
{
    [Option("source", Required = false, Default = "./", HelpText = "File or directory that represents the source files.")] public string Source { get; set; } = "./";
    [Option("target", Required = false, Default = "./", HelpText = "Directory to store the resulting files to.")] public string Target { get; set; } = "./";
    [Option("title", Required = false, Default = "", HelpText = "The title of the document. If empty, the file name will be used.")] public string Title { get; set; } = "";
    [Option("include-html", Required = false, Default = false, HelpText = "Saves the HTML version of the content into a file.")] public bool IncludeHtml { get; set; } = false;
    [Option("placeholders", Required = false, Default = "placeholders.json", HelpText = "Path to an optional JSON file that contains placeholders to be merged into the resulting document.")] public string? PlaceholdersFile { get; set; } = null;

}