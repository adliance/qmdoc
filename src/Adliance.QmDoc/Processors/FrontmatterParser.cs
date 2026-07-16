using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Adliance.QmDoc.Processors.MarkdownProcessors;
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
    public int? PdfWidth { get; set; }
    public int? PdfHeight { get; set; }
    public double? PdfScale { get; set; }
    public bool? EnableHeader { get; set; }
    public bool? EnableFooter { get; set; }
    public bool? EnableHeaderNumbering { get; set; }
    public bool? EnableDocumentTitle { get; set; }

    /// <summary>
    /// All key/value pairs found in the frontmatter (including the ones already exposed as typed properties above),
    /// usable as {{KEY}} placeholders in the document.
    /// </summary>
    public IReadOnlyDictionary<string, string> Custom { get; set; } = new Dictionary<string, string>();

    public string MarkdownWithoutFrontmatter = "";
}

public static class FrontmatterParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly IDeserializer RawDeserializer = new DeserializerBuilder().Build();

    public static Frontmatter Parse(MarkdownProcessorContext markdownContext)
    {
        var markdown = markdownContext.SourceMarkdown;
        var document = Markdown.Parse(markdown, markdownContext.Pipeline);
        var block = document.OfType<YamlFrontMatterBlock>().FirstOrDefault();

        if (block == null)
        {
            return new Frontmatter
            {
                MarkdownWithoutFrontmatter = markdown,
                Theme = null
            };
        }

        var sb = new StringBuilder();
        for (var i = 0; i < block.Lines.Count; i++) sb.AppendLine(block.Lines.Lines[i].ToString());
        var yaml = sb.ToString();

        var result = Deserializer.Deserialize<Frontmatter>(yaml);
        result.Custom = ParseAllValues(yaml);
        result.MarkdownWithoutFrontmatter = markdown[(block.Span.End + 1)..].TrimStart('\n');
        return result;
    }

    private static IReadOnlyDictionary<string, string> ParseAllValues(string yaml)
    {
        var raw = RawDeserializer.Deserialize<Dictionary<string, object>>(yaml) ?? new Dictionary<string, object>();
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in raw)
        {
            if (value is IEnumerable && value is not string) continue; // skip lists/nested mappings, only scalars are supported as placeholders
            result[key] = value.ToString() ?? "";
        }

        return result;
    }
}
