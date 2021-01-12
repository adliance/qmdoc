using System;
using Markdig;
using System.Collections.Generic;
using System.IO;
using Adliance.QmDoc.AfterConversionToHtml;
using Adliance.QmDoc.BeforeConversionToHtml;
using Adliance.QmDoc.Themes;
using HeaderNumbering = Adliance.QmDoc.AfterConversionToHtml.HeaderNumbering;
using TitlePlaceholder = Adliance.QmDoc.BeforeConversionToHtml.TitlePlaceholder;

namespace Adliance.QmDoc
{
    public static class MarkdownToHtmlConverter
    {
        public static string ConvertMarkdownToHtml(
            string theme,
            string sourceFilePath,
            string title,
            bool disableHeaderNumbering,
            DateTime? ignoreGitCommitsSince,
            out List<ProcessorError> errors)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Source file not found", sourceFilePath);
            }

            var markdown = File.ReadAllText(sourceFilePath);

            IBeforeConversionToHtmlStep[] steps =
            {
                new TitlePlaceholder(title),
                new GitVersionsPlaceholder(sourceFilePath, ignoreGitCommitsSince),
                new LinkToChapters(),
                new PageBreak(),
                new ImagesMustNotContainSpaces(sourceFilePath),
                new LinkToDocuments(sourceFilePath),
                new LinkedDocumentsPlaceholder(sourceFilePath) // add after the "LinkToDocuments" step, because that one fills the context with the linked documents 
            };

            var context = new Context();
            errors = new List<ProcessorError>();
            foreach (var step in steps)
            {
                var stepResult = step.Apply(markdown, context);
                context = stepResult.Context;
                errors.AddRange(stepResult.Errors);
                markdown = stepResult.ResultingMarkdown;
            }

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var html = Markdown.ToHtml(markdown, pipeline);

            return WrapInHtmlDocument(theme, sourceFilePath, html, title, disableHeaderNumbering, ref errors);
        }

        private static string WrapInHtmlDocument(string theme, string sourceFilePath, string html, string title, bool disableHeaderNumbering, ref List<ProcessorError> errors)
        {
            var layout = ThemeProvider.GetContent(theme);

            IAfterConversionToHtmlStep[] steps =
            {
                new BodyPlaceholder(html), // should be the first step
                new CssPlaceholder(theme),
                new AfterConversionToHtml.TitlePlaceholder(title),
                new DatePlaceholder(),
                new AuthorLine(),
                new HeaderNumbering(!disableHeaderNumbering),
                new IconBlocks(),
                new IconLists(),
                new SetCorrectChaptersLinkTitle(sourceFilePath),
                new EmbedImages(sourceFilePath)
            };

            var result = layout;
            foreach (var step in steps)
            {
                var stepResult = step.Apply(result);
                errors.AddRange(stepResult.Errors);
                result = stepResult.ResultingHtml;
            }

            return result;
        }
    }
}