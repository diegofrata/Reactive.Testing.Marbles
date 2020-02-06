using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Reactive.Testing.Marbles
{
    static class MarbleSpecParser
    {
        static readonly Regex Parser = new Regex(@"^(.+):(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

        public static Dictionary<string, string> Parse(string doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var match = Parser.Match(doc);

            var dictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            while (match.Success)
            {
                var key = match.Groups[1].Value.Trim();
                var value = match.Groups[2].Value.Trim();

                dictionary.Add(key, value);

                match = match.NextMatch();
            }

            return dictionary;
        }
    }
}