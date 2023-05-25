using System;
using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Parameters;

[Verb("pdf", true, HelpText = "Runs the full Markdown to PDF conversion.")]
public class PdfParameters : CommonConversionParameters
{
    [Option("disable-header-numbering", Required = false, Default = false, HelpText = "Disables numbering of headers.")] public bool DisableHeaderNumbering { get; set; } = false;
    [Option("exclude-pdf", Required = false, Default = false, HelpText = "The PDF version should not be saved.")] public bool ExcludePdf { get; set; } = false;
    [Option("theme", Required = false, Default = "", HelpText = "The name of the theme that should be used.")] public string Theme { get; set; } = "";
    [Option("ignore-git-commits-since", Required = false, Default = null, HelpText = "All Git commits before this date will not be shown in Git Placeholders (eg. \"2022-12-31\").")] public DateTime? IgnoreGitCommitsSince { get; set; } = null;
    [Option("ignore-git-commits", Required = false, Default = null, HelpText = "A list of Git commit SHAs or parts of Git SHAs (comma separated) that should not be shown in Git Placeholders (eg. \"d53ba669, b0d71fe388\").")] public string? IgnoreGitCommits { get; set; } = null;
    [Option("ignore-git-commits-without", Required = false, Default = null, HelpText = "A list of words (comma separated). If a commit message does not contain any of these words, the Git commit will not be shown in Git Placeholders (eg. \"Freigabe, For Training\")")] public string? IgnoreGitCommitsWithout { get; set; } = null;
}