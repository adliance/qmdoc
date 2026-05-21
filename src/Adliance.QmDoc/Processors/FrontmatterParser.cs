using System.Linq;
using System.Text;
using Markdig;
using Markdig.Extensions.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Adliance.QmDoc.Processors;

public class Frontmatter
{
    public string? Title { get; set; }
    public string? Theme { get; set; }
    public string? Author { get; set; }
    public string MarkdownWithoutFrontmatter = "";
}

public static class FrontmatterParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static Frontmatter Parse(MarkdownPipeline pipeline, string markdown)
    {
        var document = Markdown.Parse(markdown, pipeline);
        var block = document.OfType<YamlFrontMatterBlock>().FirstOrDefault();

        if (block == null)
        {
            return new Frontmatter
            {
                MarkdownWithoutFrontmatter = markdown,
                Theme = "Default"
            };
        }

        var sb = new StringBuilder();
        for (var i = 0; i < block.Lines.Count; i++) sb.AppendLine(block.Lines.Lines[i].ToString());

        var result = Deserializer.Deserialize<Frontmatter>(sb.ToString());
        result.MarkdownWithoutFrontmatter = markdown[(block.Span.End + 1)..].TrimStart('\n');

        Program.WriteLine($"""
                           {"\t"}Frontmatter configuration found:
                           {"\t\t"}Title: {result.Title}
                           {"\t\t"}Theme: {result.Theme}
                           {"\t\t"}Author: {result.Author}
                           """);

        return result;
    }
}
