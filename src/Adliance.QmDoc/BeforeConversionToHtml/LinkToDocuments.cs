using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml
{
    public class LinkToDocuments : IBeforeConversionToHtmlStep
    {
        private readonly string _baseDirectory;
        private readonly string _filePath;

        public LinkToDocuments(string baseDirectory, string filePath)
        {
            _baseDirectory = baseDirectory;
            _filePath = filePath;
        }

        public Result Apply(string markdown, Context context)
        {
            var result = new Result(markdown, context);

            var resultingMarkdown = ReplaceDocumentsByFileExtension(result, markdown, context);
            resultingMarkdown = ReplaceDocumentsByCode(result, resultingMarkdown, context);

            result.ResultingMarkdown = resultingMarkdown;
            return result;
        }

        private string ReplaceDocumentsByCode(Result result, string markdown, Context context)
        {
            var resultingMarkdown = markdown;
            var matches = Regex.Matches(markdown, @"\[(\w\w\w\-\d\d\d)\]");

            foreach (Match? m in matches)
            {
                if (m == null) continue;

                var code = m.Groups[1].Value;

                var allFiles = Directory.GetFiles(Path.GetDirectoryName(_baseDirectory) ?? "", "*.md", SearchOption.AllDirectories).ToList();
                var linkedFilePath = allFiles.FirstOrDefault(x => Path.GetFileName(x).StartsWith(code + " ", true, CultureInfo.InvariantCulture));
                if (linkedFilePath == null)
                {
                    result.Errors.Add(new ProcessorError(_filePath, $"Unable to find a document \"{code}\", but there's a referenced document number to it."));
                }
                else
                {
                    var relativeFilePath = Path.GetRelativePath(Path.GetDirectoryName(_filePath)!, linkedFilePath).Replace("\\", "/");
                    var targetFilePath = relativeFilePath.Replace(" ", "%20").Replace(".md", ".html");
                    var linkedDocument = new LinkedDocument(targetFilePath, Path.GetFileNameWithoutExtension(relativeFilePath));
                    context.LinkedDocuments.Add(linkedDocument);
                    resultingMarkdown = resultingMarkdown.Replace($"[{code}]", $"<span class=\"link-to-document\"><i></i>[{linkedDocument.NiceName}]({linkedDocument.FileName})</span>");
                }
            }

            return resultingMarkdown;
        }

        private string ReplaceDocumentsByFileExtension(Result result, string markdown, Context context)
        {
            var resultingMarkdown = markdown;
            var matches = Regex.Matches(markdown, @"\[([^\]]*?\.\w{1,4})\]");

            foreach (Match? m in matches)
            {
                if (m == null) continue;

                var linkedFileName = m.Groups[1].Value;
                linkedFileName = linkedFileName.Replace("%20", " ");
                if (linkedFileName.Contains("@")) continue;

                var matchingFile = new FileInfo(Path.Combine(Path.GetDirectoryName(_filePath)!, linkedFileName));
                if (!matchingFile.Exists)
                {
                    result.Errors.Add(new ProcessorError(_filePath, $"Unable to find a document \"{linkedFileName}\", but there's a reference to it."));
                }
                else
                {
                    var fileName = linkedFileName;

                    if (Path.GetExtension(fileName).Equals(".md", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName = fileName.Replace(".md", ".html");
                    }

                    var linkedDocument = new LinkedDocument(fileName.Replace(" ", "%20"), Path.GetFileNameWithoutExtension(fileName.Replace("%20", " ")));
                    context.LinkedDocuments.Add(linkedDocument);
                    resultingMarkdown = resultingMarkdown.Replace($"[{linkedFileName}]", $"<span class=\"link-to-document\"><i></i>[{linkedDocument.NiceName}]({linkedDocument.FileName})</span>");
                }
            }

            return resultingMarkdown;
        }
    }
}