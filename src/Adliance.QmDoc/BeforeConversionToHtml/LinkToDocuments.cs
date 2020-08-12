using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml
{
    public class LinkToDocuments : IBeforeConversionToHtmlStep
    {
        private readonly string _filePath;

        public LinkToDocuments(string filePath)
        {
            _filePath = filePath;
        }

        public Result Apply(string markdown, Context context)
        {
            var result = new Result(markdown, context);

            var resultingMarkdown = ReplaceDocumentsByCode(result, markdown, context);
            resultingMarkdown = ReplaceDocumentsByFileExtension(result, resultingMarkdown, context);

            result.ResultingMarkdown = resultingMarkdown;
            return result;
        }

        private string ReplaceDocumentsByCode(Result result, string markdown, Context context)
        {
            var resultingMarkdown = markdown;
            var matches = Regex.Matches(markdown, @"\[(\w\w\w\-\d\d\d)\]");

            foreach (Match? m in matches)
            {
                if (m==null) continue;
                
                var code = m.Groups[1].Value;

                var allFiles = Directory.GetFiles(Path.GetDirectoryName(_filePath), "*.md").ToList();
                var fileName = allFiles.Select(Path.GetFileNameWithoutExtension).FirstOrDefault(x => x!=null && x.StartsWith(code + " ", true, CultureInfo.InvariantCulture));
                if (fileName == null)
                {
                    result.Errors.Add(new ProcessorError(_filePath, $"Unable to find a document \"{code}\", but there's a reference to it."));
                }
                else
                {
                    var linkedDocument = new LinkedDocument(fileName.Replace(" ", "%20") + ".html", fileName);
                    context.LinkedDocuments.Add(linkedDocument);
                    resultingMarkdown = resultingMarkdown.Replace($"[{code}]", $"*[{linkedDocument.NiceName}]({linkedDocument.FileName})*");
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

                var allFiles = Directory.GetFiles(Path.GetDirectoryName(_filePath), "*.*", SearchOption.AllDirectories).ToList();
                var matchingFile = allFiles.FirstOrDefault(x => x.EndsWith(linkedFileName.Replace("%20", " ").Replace('/', Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));
                if (matchingFile == null)
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
                    resultingMarkdown = resultingMarkdown.Replace($"[{linkedFileName}]", $"*[{linkedDocument.NiceName}]({linkedDocument.FileName})*");
                }
            }

            return resultingMarkdown;
        }
    }
}
