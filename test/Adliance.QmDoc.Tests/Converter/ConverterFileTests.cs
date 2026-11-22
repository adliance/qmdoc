using Adliance.QmDoc.Converter;

namespace Adliance.QmDoc.Tests.Converter;

public class ConverterFileTests
{
    [Theory]
    [InlineData(TargetExtension.Pdf, "doc.pdf")]
    [InlineData(TargetExtension.Html, "doc.html")]
    [InlineData(TargetExtension.Docx, "doc.docx")]
    public void TargetAbsolutePath_ChangesExtension(TargetExtension extension, string expectedFileName)
    {
        var source = Path.Combine(Path.GetTempPath(), "source");
        var target = Path.Combine(Path.GetTempPath(), "target");
        var file = new ConverterFile(source, "doc.md", Path.Combine(source, "doc.md"), target, extension);

        Assert.Equal(source, file.SourceBaseDirectory);
        Assert.Equal("doc.md", file.SourceRelativePath);
        Assert.Equal(Path.Combine(source, "doc.md"), file.SourceAbsolutePath);
        Assert.Equal(Path.Combine(target, expectedFileName), file.TargetAbsolutePath);
    }

    [Fact]
    public void TargetAbsolutePath_KeepsSubdirectoriesAndDotsInFileName()
    {
        var source = Path.Combine(Path.GetTempPath(), "source");
        var target = Path.Combine(Path.GetTempPath(), "target");
        var relative = Path.Combine("sub", "report.de.md");
        var file = new ConverterFile(source, relative, Path.Combine(source, relative), target, TargetExtension.Pdf);
        Assert.Equal(Path.Combine(target, "sub", "report.de.pdf"), file.TargetAbsolutePath);
    }

    [Fact]
    public void TargetAbsolutePath_IsAbsoluteForRelativeTarget()
    {
        var file = new ConverterFile("source", "doc.md", Path.Combine("source", "doc.md"), "./", TargetExtension.Html);
        Assert.True(Path.IsPathRooted(file.TargetAbsolutePath));
        Assert.Equal(Path.Combine(Environment.CurrentDirectory, "doc.html"), file.TargetAbsolutePath);
    }
}
