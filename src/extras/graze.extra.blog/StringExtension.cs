using System;

namespace graze.extra.childpages
{
    public static class StringExtension
    {
        public static string GetTagsValue(this string content, string tag)
        {
            var startingPos = content.IndexOf(tag) + tag.Length + 1;
            var nextLineEnding = content.IndexOf(Environment.NewLine, startingPos);

            var line = content.Substring(startingPos, nextLineEnding - startingPos);

            return line.Trim();  
        }
    }
}