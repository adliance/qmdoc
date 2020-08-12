using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml
{
    public class ImagesMustNotContainSpaces : IBeforeConversionToHtmlStep
    {
        private readonly string _sourceFilePath;

        public ImagesMustNotContainSpaces(string sourceFilePath)
        {
            _sourceFilePath = sourceFilePath;
        }

        public Result Apply(string markdown, Context context)
        {
            var result = new Result(markdown, context);
            foreach (Match? m in Regex.Matches(markdown, "\\!\\[.*?\\]\\((.* .*)\\)", RegexOptions.IgnoreCase))
            {
                if (m == null) continue;
                result.Errors.Add(new ProcessorError(_sourceFilePath, $"'{m.Groups[1].Value}' contains spaces, but image URLs must not contain spaces."));
            }

            return result;
        }
    }
}
