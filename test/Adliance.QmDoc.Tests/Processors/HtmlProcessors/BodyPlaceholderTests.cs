using Adliance.QmDoc.Processors.HtmlProcessors;

namespace Adliance.QmDoc.Tests.Processors.HtmlProcessors;

public class BodyPlaceholderTests
{
    [Theory]
    [InlineData("{{body}}")]
    [InlineData("{{ BODY }}")]
    [InlineData("{{  Body  }}")]
    public void Apply_ReplacesBody(string placeholder)
    {
        Assert.Equal("<body><p>Text</p></body>", new BodyPlaceholder("<p>Text</p>").Apply($"<body>{placeholder}</body>").ResultingHtml);
    }

    [Fact]
    public void Apply_BodyWithDollarSigns_IsInsertedLiterally()
    {
        const string body = "<code>echo $$; price: $0 or $&amp; or $1 or ${name}</code>";
        Assert.Equal(body, new BodyPlaceholder(body).Apply("{{body}}").ResultingHtml);
    }
}
