using System.Text;

namespace Adliance.QmDoc.Tests.Helpers;

/// <summary>
/// A temporary working directory that is deleted after the test.
/// It's nested one level deep, because some processors also search the parent of the source directory.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    private readonly string _root;

    public TempDirectory()
    {
        _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qmdoc-tests", Guid.NewGuid().ToString("N"));
        Path = System.IO.Path.Combine(_root, "work");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public string Write(string relativePath, string content)
    {
        var fullPath = Combine(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content, new UTF8Encoding(false));
        return fullPath;
    }

    public string Write(string relativePath, byte[] content)
    {
        var fullPath = Combine(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, content);
        return fullPath;
    }

    public string Read(string relativePath) => File.ReadAllText(Combine(relativePath));

    public bool Exists(string relativePath) => File.Exists(Combine(relativePath));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // best effort, the OS cleans up the temp directory eventually
        }
        catch (UnauthorizedAccessException)
        {
            // e.g. read-only files created by Git
        }
    }
}
