using System;
using CommandLine;

namespace Adliance.QmDoc.Parameters;

public abstract class CommonConversionParameters
{
    [Option("source", Required = false, Default = "./", HelpText = "File or directory that represents the source files.")] public string Source { get; set; } = "./";
    [Option("target", Required = false, Default = "./", HelpText = "Directory to store the resulting files to.")] public string Target { get; set; } = "./";
    [Option("title", Required = false, Default = "", HelpText = "The title of the document. If empty, the file name will be used.")] public string Title { get; set; } = "";
    [Option("placeholders", Required = false, Default = "placeholders.json", HelpText = "Path to an optional JSON file that contains placeholders to be merged into the resulting document.")] public string? PlaceholdersFile { get; set; } = null;
    [Option("ignore-git-commits-since", Required = false, Default = null, HelpText = "All Git commits before this date will not be shown in Git Placeholders (eg. \"2022-12-31\").")] public DateTime? IgnoreGitCommitsSince { get; set; } = null;
    [Option("ignore-git-commits", Required = false, Default = null, HelpText = "A list of Git commit SHAs or parts of Git SHAs (comma separated) that should not be shown in Git Placeholders (eg. \"d53ba669, b0d71fe388\").")] public string? IgnoreGitCommits { get; set; } = null;
    [Option("ignore-git-commits-without", Required = false, Default = null, HelpText = "A list of words (comma separated). If a commit message does not contain any of these words, the Git commit will not be shown in Git Placeholders (eg. \"Freigabe, For Training\")")] public string? IgnoreGitCommitsWithout { get; set; } = null;
    [Option("include-subdirectories", Required = false, Default = false, HelpText = "Also looks into all subdirectories of the source directory for Markdown files, instead of just the source directory itself.")] public bool IncludeSubdirectories { get; set; } = false;
}
