using System;
using System.Collections.Generic;

namespace graze.extra.childpages
{
    public static class StringExtension
    {
        private static string GetTagsValue(this string content, string tag)
        {
            var metaLines = content.Split(new[] { Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);

            var tags = new Dictionary<string, string>();

            foreach (var metaLine in metaLines)
            {
                var key = metaLine.Substring(0, metaLine.IndexOf(":")).Trim();
                var value = metaLine.Substring(metaLine.IndexOf(":")+1).Trim();

                tags.Add(key, value);
            }

            return !tags.ContainsKey(tag) ? "" : tags[tag];
        }

        public static Dictionary<string, string> GetTags(this string content)
        {
            Console.WriteLine(content);

            var metaLines = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            Console.WriteLine("Meta lines: " + metaLines.Length);

            var tags = new Dictionary<string, string>();

            foreach (var metaLine in metaLines)
            {
                var key = metaLine.Substring(0, metaLine.IndexOf(":", StringComparison.Ordinal)).Trim();
                if (metaLine.Length <= metaLine.IndexOf(":", StringComparison.Ordinal) + 1) continue;

                var value = metaLine.Substring(metaLine.IndexOf(":") + 1).Trim();

                tags.Add(key, value);

                Console.WriteLine(key + " - " + value);
            }

            return tags;
        }
    }
}