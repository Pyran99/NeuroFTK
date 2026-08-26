#nullable enable

using System.Text;
using System.Text.RegularExpressions;

namespace Pyran.NeuroFTK.Utils
{
    public class StringReplace
    {
        public static string ReplaceNewLine(string input)
        {
            return input.Replace("\r\n", ", ").Replace("\n", ", ");
            // StringBuilder builder = new(input.Length);
            // foreach (char c in input)
            // {
            //     if (c == '\n')
            //     {
            //         builder.Append(", ");
            //         continue;
            //     }
            //     builder.Append(c);
            // }
            // return builder.ToString();
        }

        public static string ReplaceNewLineSpace(string input)
        {
            return input.Replace("\r\n", " ").Replace("\n", " ");
            // return Regex.Replace(input, @"\r\n?|\n", " ");
        }

        public static string RemoveStyling(string? input)
        {
            if (input == null) return "";
            Regex pattern = new(@"<[^>]*>");
            if (pattern.IsMatch(input))
            {
                input = pattern.Replace(input, "");
            }
            return input;
        }
    }
}