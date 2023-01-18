using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Adliance.QmDoc
{
    public static class GitService
    {
        public static Change? GetLatestVersion(string sourceFilePath, DateTime? ignoreGitCommitsSince, IList<string> ignoreCommits, IList<string> requiredWordsForCommit)
        {
            var versions = GetVersions(sourceFilePath, ignoreGitCommitsSince, ignoreCommits, requiredWordsForCommit);
            return versions.FirstOrDefault();
        }

        public static IList<Change> GetVersions(string sourceFilePath, DateTime? ignoreGitCommitsSince, IList<string> ignoreCommits, IList<string> ignoreCommitsWithout)
        {
            var result = new List<Change>();

            sourceFilePath = new FileInfo(sourceFilePath).FullName;
            var repoPath = GetRepoDirectory(Path.GetDirectoryName(sourceFilePath) ?? "");
            if (string.IsNullOrWhiteSpace(repoPath))
            {
                return result;
            }

            var relativeFilePath = sourceFilePath.Substring(repoPath.Length).Trim(Path.DirectorySeparatorChar);

            using (var repo = new Repository(repoPath))
            {
                var filter = new CommitFilter { SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time };

                foreach (var commit in repo.Commits.QueryBy(filter))
                {
                    foreach (var parent in commit.Parents.Take(1))
                    {
                        foreach (var change in repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree))
                        {
                            if (ignoreCommitsWithout.Any() && !ignoreCommitsWithout.Any(x => commit.Message != null && commit.Message.Contains(x, StringComparison.OrdinalIgnoreCase))) continue;
                            if (ignoreCommits.Any() && ignoreCommits.Any(x => commit.Sha.Contains(x, StringComparison.OrdinalIgnoreCase))) continue;

                            if (change.Path.Replace('/', Path.DirectorySeparatorChar).Equals(relativeFilePath, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add(new Change
                                {
                                    Author = commit.Committer.Name,
                                    Date = commit.Committer.When,
                                    Message = (commit.Message ?? "").Trim(),
                                    MessageShort = (commit.MessageShort ?? "").Trim(),
                                    Sha = commit.Sha
                                });
                            }
                        }
                    }
                }
            }

            result = result.ToList();

            return result.Where(x => !x.Message.StartsWith("Merge", StringComparison.OrdinalIgnoreCase))
                .Where(x => !ignoreGitCommitsSince.HasValue || x.Date < ignoreGitCommitsSince.Value)
                .OrderByDescending(x => x.Date)
                .ToList();
        }

        public static string? GetRepoDirectory(string sourceDirectory)
        {
            var directory = new DirectoryInfo(sourceDirectory);

            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            if (directory.Parent != null)
            {
                var recursiveResult = GetRepoDirectory(directory.Parent.FullName);
                if (!string.IsNullOrWhiteSpace(recursiveResult))
                {
                    return recursiveResult;
                }
            }

            return null;
        }

        public class Change
        {
            public string Author { get; set; } = "";
            public DateTimeOffset Date { get; set; }
            public string Sha { get; set; } = "";
            public string ShaShort => Sha.Length >= 7 ? Sha.Substring(0, 7) : "";
            public string Message { get; set; } = "";
            public string MessageShort { get; set; } = "";
        }
    }
}