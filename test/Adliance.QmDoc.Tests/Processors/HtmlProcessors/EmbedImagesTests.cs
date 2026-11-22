using Adliance.QmDoc.Processors.HtmlProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.HtmlProcessors;

public sealed class EmbedImagesTests : IDisposable
{
    private static readonly byte[] Bytes = [1, 2, 3, 4];
    private static readonly string Base64 = Convert.ToBase64String(Bytes);

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private HtmlProcessorResult Embed(string html) => new EmbedImages(_dir.Combine("doc.md")).Apply(html);

    [Theory]
    [InlineData("image.png", "image/png")]
    [InlineData("image.PNG", "image/png")]
    [InlineData("image.svg", "image/svg+xml")]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("image.jpeg", "image/jpeg")]
    [InlineData("image.gif", "image/jpeg")] // everything else is treated as JPEG
    public void Apply_EmbedsLocalImages(string fileName, string mimeType)
    {
        _dir.Write(fileName, Bytes);
        var result = Embed($"<p><img src=\"{fileName}\" alt=\"x\" /></p>");
        Assert.Equal($"<p><img src=\"data:{mimeType};base64,{Base64}\" alt=\"x\" /></p>", result.ResultingHtml);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Apply_EmbedsImagesInSubdirectories()
    {
        _dir.Write("images/chart.png", Bytes);
        Assert.Equal($"<img src=\"data:image/png;base64,{Base64}\">", Embed("<img src=\"images/chart.png\">").ResultingHtml);
    }

    [Fact]
    public void Apply_SameImageMultipleTimes()
    {
        _dir.Write("a.png", Bytes);
        var result = Embed("<img src=\"a.png\"><img src=\"a.png\">");
        Assert.Equal($"<img src=\"data:image/png;base64,{Base64}\"><img src=\"data:image/png;base64,{Base64}\">", result.ResultingHtml);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Apply_MissingImage_ReportsError()
    {
        var result = Embed("<img src=\"missing.png\">");
        Assert.Equal("<img src=\"missing.png\">", result.ResultingHtml);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Unable to find image 'missing.png'.", error.ErrorMessage);
        Assert.False(error.IsWarningOnly);
    }

    [Fact]
    public void Apply_ImageWithSpacesInFileName()
    {
        _dir.Write("my image.png", Bytes);
        var result = Embed("<img src=\"my%20image.png\">");
        Assert.Equal($"<img src=\"data:image/png;base64,{Base64}\">", result.ResultingHtml);
    }

    [Theory]
    [InlineData("https://example.com/logo.png")]
    [InlineData("HTTP://example.com/logo.png")]
    [InlineData("//example.com/logo.png")]
    [InlineData("data:image/png;base64,AQID")]
    public void Apply_RemoteImages_AreLeftAlone(string src)
    {
        var html = $"<img src=\"{src}\">";
        var result = Embed(html);
        Assert.Equal(html, result.ResultingHtml);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Apply_ImageWithEncodedNonAsciiCharactersAndAmpersand()
    {
        _dir.Write("Bild ä & ö.png", Bytes);
        var result = Embed("<img src=\"Bild%20%C3%A4%20&amp;%20%C3%B6.png\">");
        Assert.Equal($"<img src=\"data:image/png;base64,{Base64}\">", result.ResultingHtml);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Apply_MissingImageWithSpaces_ReportsDecodedFileName()
    {
        Assert.Equal("Unable to find image 'my image.png'.", Assert.Single(Embed("<img src=\"my%20image.png\">").Errors).ErrorMessage);
    }

    [Fact]
    public void Apply_MissingAndExistingImages()
    {
        _dir.Write("a.png", Bytes);
        var result = Embed("<img src=\"missing.png\"><img src=\"a.png\">");
        Assert.Equal($"<img src=\"missing.png\"><img src=\"data:image/png;base64,{Base64}\">", result.ResultingHtml);
        Assert.Single(result.Errors);
    }
}
