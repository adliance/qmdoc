using System;
using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Configuration
{
    [Verb("run", true, HelpText = "Runs the full Markdown to PDF conversion.")]
    public class RunParameters
    {
        [Option("source", Required = false, Default = "./", HelpText = "File or directory that represents the source files.")] public string Source { get; set; } = "./";

        [Option("target", Required = false, Default = "./", HelpText = "Directory to store the resulting files to.")] public string Target { get; set; } = "./";

        [Option("disable-header-numbering", Required = false, Default = false, HelpText = "Disables numbering of headers.")] public bool DisableHeaderNumbering { get; set; } = false;

        [Option("title", Required = false, Default = "", HelpText = "The title of the document. If empty, the file name will be used.")] public string Title { get; set; } = "";

        [Option("include-html", Required = false, Default = false, HelpText = "Saves the HTML version of the content into a file.")] public bool IncludeHtml { get; set; } = false;

        [Option("exclude-pdf", Required = false, Default = false, HelpText = "The PDF version should not be saved.")] public bool ExcludePdf { get; set; } = false;

        [Option("theme", Required = false, Default = "", HelpText = "The name of the theme that should be used.")] public string Theme { get; set; } = "";
            
        [Option("ignore-git-commits-since", Required = false, Default = null, HelpText = "The name of the theme that should be used.")] public DateTime? IgnoreGitCommitsSince { get; set; } = null;
    }

    [Verb("set-theme", HelpText = "Sets the theme that will be used for all conversions.")]
    public class SetThemeParameters
    {
        [Option("theme", Required = true, Default = "", HelpText = "The name of the theme that should be used.")] public string Theme { get; set; } = "";
    }
}