using System;
using System.Collections.Generic;
using System.Linq;

namespace Adliance.QmDoc.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveMultipleNbsp(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return s;
            }

            return s.Replace("&nbsp;&nbsp;&nbsp;", "&nbsp;", StringComparison.OrdinalIgnoreCase)
                .Replace("&nbsp;&nbsp;", "&nbsp;", StringComparison.OrdinalIgnoreCase);
        }

        public static IList<string> SplitCleanOrder(this string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new List<string>();
            return s.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
        }
    }
}