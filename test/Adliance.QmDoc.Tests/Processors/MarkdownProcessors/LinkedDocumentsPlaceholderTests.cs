using Adliance.QmDoc.Processors.MarkdownProcessors;
using Adliance.QmDoc.Tests.Helpers;

namespace Adliance.QmDoc.Tests.Processors.MarkdownProcessors;

public class LinkedDocumentsPlaceholderTests
{
    [Fact]
    public void Apply_ListsDistinctDocumentsOrderedByName()
    {
        var context = Md.Context("{{ LINKED_DOCUMENTS }}");
        context.LinkedDocuments.Add(new LinkedDocument("b.html", "B"));
        context.LinkedDocuments.Add(new LinkedDocument("a.html", "A"));
        context.LinkedDocuments.Add(new LinkedDocument("B.HTML", "b"));

        var result = new LinkedDocumentsPlaceholder().Apply(context).Markdown;

        Assert.Equal(
            "* <span class=\"link-to-document\"><i></i>[A](a.html)</span>\n* <span class=\"link-to-document\"><i></i>[B](b.html)</span>",
            result);
    }

    [Fact]
    public void Apply_WithoutDocuments_RemovesPlaceholder()
    {
        Assert.Equal("Docs: ", new LinkedDocumentsPlaceholder().Apply("Docs: {{ LINKED_DOCUMENTS }}").Markdown);
    }
}
