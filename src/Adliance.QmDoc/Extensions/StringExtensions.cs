using System;

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
    }
}