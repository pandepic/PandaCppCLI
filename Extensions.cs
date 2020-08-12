using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PandaCppCLI
{
    public static class Extensions
    {
        public static string ToLowercaseNamingConvention(this string s)
        {
            var r = new Regex(@"
            (?<=[A-Z])(?=[A-Z][a-z]) |
                (?<=[^A-Z])(?=[A-Z]) |
                (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

            return r.Replace(s, "_").ToLower();
        }
    }
}
