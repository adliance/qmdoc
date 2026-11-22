namespace Adliance.QmDoc.Tests;

public class ProcessorErrorTests
{
    [Fact]
    public void Constructor_ExtractsFileName()
    {
        var path = Path.Combine("docs", "sub", "doc.md");
        var error = new ProcessorError(path, "Message", true);
        Assert.Equal("doc.md", error.FileName);
        Assert.Equal(path, error.FilePath);
        Assert.Equal("Message", error.ErrorMessage);
        Assert.True(error.IsWarningOnly);
    }

    [Fact]
    public void Equality_ComparesAllValues()
    {
        var a = new ProcessorError("doc.md", "Message");
        Assert.Equal(a, new ProcessorError("doc.md", "Message"));
        Assert.Equal(a.GetHashCode(), new ProcessorError("doc.md", "Message").GetHashCode());
        Assert.NotEqual(a, new ProcessorError("doc.md", "Other"));
        Assert.NotEqual(a, new ProcessorError("other.md", "Message"));
        Assert.NotEqual(a, new ProcessorError("doc.md", "Message", true));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("doc.md"));
    }
}
