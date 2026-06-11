using System.Text;

namespace Pyran.NeuroFTK
{
    public class StringReplace
    {
        public static string ReplaceNewLine(string input)
        {
            StringBuilder builder = new(input.Length);
            foreach (char c in input)
            {
                if (c != '\n') builder.Append(c);
            }
            return builder.ToString();
        }
    }
}