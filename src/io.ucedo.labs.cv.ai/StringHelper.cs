using System.Text.RegularExpressions;

namespace io.ucedo.labs.cv.ai
{
    public static class StringHelper
    {
        public static IDictionary<string, string> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, string>();
            var regex = new Regex(@"(?<key>\w+)=((?<value>'[^']*')|(?<value>[^&]*))");
            var matches = regex.Matches(queryString);

            foreach (Match match in matches)
            {
                var key = match.Groups["key"].Value;
                var value = match.Groups["value"].Value;

                result[key] = value.Trim('\'');
            }

            return result;
        }

        public static IEnumerable<string> ParseText(string text, string separator, int groupSize)
        {

            var parts = text.Split(new[] { separator }, StringSplitOptions.None).ToList();
            for (var i = 0; i < parts.Count - 1; i++)
                parts[i] = parts[i] + separator;

            List<string> combined = new();

            for (int i = 0; i < parts.Count; i += groupSize)
            {
                int endIndex = Math.Min(i + groupSize, parts.Count);
                combined.Add(string.Join(string.Empty, parts.GetRange(i, endIndex - i)));
            }

            return combined;
        }
    }
}