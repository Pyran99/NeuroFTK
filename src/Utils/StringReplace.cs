using System.Text;
using System.Text.RegularExpressions;

namespace Pyran.NeuroFTK
{
    public class StringReplace
    {
        public static string ReplaceNewLine(string input)
        {
            StringBuilder builder = new(input.Length);
            foreach (char c in input)
            {
                if (c == '\n')
                {
                    builder.Append(", ");
                    continue;
                }
                builder.Append(c);
            }
            return builder.ToString();
        }

        public static string RemoveStyling(string input)
        {
            Regex pattern = new(@"<[^>]*>");
            if (pattern.IsMatch(input))
            {
                input = pattern.Replace(input, "");
            }
            return input;
        }
    }
}