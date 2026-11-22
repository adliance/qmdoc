using Adliance.QmDoc.Tests.Helpers;
using LibGit2Sharp;

namespace Adliance.QmDoc.Tests;

public sealed class GitServiceTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly TempDirectory _dir = new();
    private readonly Repository _repo;
    private int _commitCount;

    public GitServiceTests()
    {
        _repo = new Repository(Repository.Init(_dir.Path));
        Commit("README.md", "Readme", "Initial commit");
    }

    public void Dispose()
    {
        _repo.Dispose();
        _dir.Dispose();
    }

    private string DocPath => _dir.Combine("docs/doc.md");

    private Commit Commit(string file, string content, string message, string author = "Hannes Sachsenhofer")
    {
        _dir.Write(file, content);
        Commands.Stage(_repo, file);
        var signature = new Signature(author, "test@example.com", Start.AddDays(_commitCount++));
        return _repo.Commit(message, signature, signature);
    }

    [Fact]
    public void GetVersions_ReturnsChangesOfFileNewestFirst()
    {
        var first = Commit("docs/doc.md", "v1", "Add document");
        Commit("docs/other.md", "x", "Other document");
        var second = Commit("docs/doc.md", "v2", "Fix typo\n\nIn chapter 2.\n", "Max Muster");

        var versions = GitService.GetVersions(DocPath, null, [], []);

        Assert.Equal(2, versions.Count);
        Assert.Equal(second.Sha, versions[0].Sha);
        Assert.Equal(second.Sha[..7], versions[0].ShaShort);
        Assert.Equal("Max Muster", versions[0].Author);
        Assert.Equal("Fix typo", versions[0].MessageShort);
        Assert.Equal("Fix typo\n\nIn chapter 2.", versions[0].Message);
        Assert.Equal(Start.AddDays(3), versions[0].Date);
        Assert.Equal(first.Sha, versions[1].Sha);
        Assert.Equal("Hannes Sachsenhofer", versions[1].Author);
    }

    [Fact]
    public void GetVersions_IgnoresMergeCommits()
    {
        Commit("docs/doc.md", "v1", "Add document");
        Commit("docs/doc.md", "v2", "Merge branch 'feature'");
        Assert.Equal(["Add document"], GitService.GetVersions(DocPath, null, [], []).Select(x => x.MessageShort));
    }

    [Fact]
    public void GetVersions_IgnoresCommitsSinceDate()
    {
        Commit("docs/doc.md", "v1", "First"); // day 1
        Commit("docs/doc.md", "v2", "Second"); // day 2
        Commit("docs/doc.md", "v3", "Third"); // day 3
        var versions = GitService.GetVersions(DocPath, Start.AddDays(2).UtcDateTime, [], []);
        Assert.Equal(["First"], versions.Select(x => x.MessageShort));
    }

    [Fact]
    public void GetVersions_IgnoresCommitsBySha()
    {
        Commit("docs/doc.md", "v1", "First");
        var second = Commit("docs/doc.md", "v2", "Second");
        var third = Commit("docs/doc.md", "v3", "Third");
        var versions = GitService.GetVersions(DocPath, null, [second.Sha[..8], third.Sha.ToUpperInvariant()], []);
        Assert.Equal(["First"], versions.Select(x => x.MessageShort));
    }

    [Fact]
    public void GetVersions_OnlyIncludesCommitsWithRequiredWords()
    {
        Commit("docs/doc.md", "v1", "Freigabe Version 1");
        Commit("docs/doc.md", "v2", "Work in progress");
        Commit("docs/doc.md", "v3", "Typo\n\nfor training");
        var versions = GitService.GetVersions(DocPath, null, [], ["freigabe", "For Training"]);
        Assert.Equal(["Typo", "Freigabe Version 1"], versions.Select(x => x.MessageShort));
    }

    [Fact]
    public void GetVersions_FilePathIsCaseInsensitive()
    {
        Commit("docs/doc.md", "v1", "First");
        Assert.Single(GitService.GetVersions(_dir.Combine("docs/doc.md"), null, [], []));
    }

    [Fact]
    public void GetVersions_FileWithoutCommits_ReturnsEmptyList()
    {
        Assert.Empty(GitService.GetVersions(_dir.Combine("docs/new.md"), null, [], []));
    }

    [Fact]
    public void GetVersions_FileOutsideOfRepository_ReturnsEmptyList()
    {
        using var other = new TempDirectory();
        var path = other.Write("doc.md", "x");
        Assert.Empty(GitService.GetVersions(path, null, [], []));
    }

    [Fact]
    public void GetVersions_InShallowClone_IgnoresCommitsAtTheBoundary()
    {
        Commit("docs/doc.md", "v1", "First");
        var second = Commit("docs/doc.md", "v2", "Second");
        Assert.Equal(2, GitService.GetVersions(DocPath, null, [], []).Count);

        // a shallow clone only knows the commits listed in ".git/shallow", but not their parents
        File.WriteAllText(Path.Combine(_dir.Path, ".git", "shallow"), second.Sha + "\n");

        Assert.Empty(GitService.GetVersions(DocPath, null, [], []));
    }

    [Fact]
    public void GetVersions_IncludesRootCommit()
    {
        using var dir = new TempDirectory();
        using var repo = new Repository(Repository.Init(dir.Path));
        dir.Write("doc.md", "v1");
        Commands.Stage(repo, "doc.md");
        var signature = new Signature("Hannes", "test@example.com", Start);
        repo.Commit("Initial commit", signature, signature);

        Assert.Single(GitService.GetVersions(dir.Combine("doc.md"), null, [], []));
    }

    [Fact]
    public void GetLatestVersion_ReturnsNewestChange()
    {
        Commit("docs/doc.md", "v1", "First");
        var second = Commit("docs/doc.md", "v2", "Second");
        Assert.Equal(second.Sha, GitService.GetLatestVersion(DocPath, null, [], [])?.Sha);
        Assert.Null(GitService.GetLatestVersion(_dir.Combine("docs/new.md"), null, [], []));
    }

    [Fact]
    public void GetRepoDirectory_FindsRepositoryInParentDirectories()
    {
        var nested = _dir.Combine("a/b/c");
        Directory.CreateDirectory(nested);
        Assert.Equal(new DirectoryInfo(_dir.Path).FullName, GitService.GetRepoDirectory(nested));
        Assert.Equal(new DirectoryInfo(_dir.Path).FullName, GitService.GetRepoDirectory(_dir.Path));
    }

    [Fact]
    public void GetRepoDirectory_OutsideOfRepository_ReturnsNull()
    {
        using var other = new TempDirectory();
        Assert.Null(GitService.GetRepoDirectory(other.Path));
    }

    [Theory]
    [InlineData("0123456789abcdef", "0123456")]
    [InlineData("0123456", "0123456")]
    [InlineData("012345", "")]
    [InlineData("", "")]
    public void Change_ShaShort(string sha, string expected)
    {
        Assert.Equal(expected, new GitService.Change { Sha = sha }.ShaShort);
    }
}
