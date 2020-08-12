using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.BeforeConversionToHtml
{
    public class DatePlaceholder : IBeforeConversionToHtmlStep
    {
        public Result Apply(string markdown, Context context)
        {
            var result = Regex.Replace(markdown, @"\{\{\W*DATE\W*\}\}", DateTime.Now.ToString("dd. MMMM yyyy", new CultureInfo("de-DE")), RegexOptions.IgnoreCase);
            return new Result(result, context);
        }
    }
}
