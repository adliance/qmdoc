using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml
{
    public class DatePlaceholder : IAfterConversionToHtmlStep
    {
        public Result Apply(string html)
        {
            var result = Regex.Replace(html, @"\{?\{\W*DATE\W*\}\}?", DateTime.Now.ToString("dd. MMMM yyyy", new CultureInfo("de-DE")), RegexOptions.IgnoreCase);
            return new Result(result);
        }
    }
}
