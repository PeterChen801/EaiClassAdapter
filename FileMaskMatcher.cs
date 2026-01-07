using System.IO;
using System.Text.RegularExpressions;

namespace EaiClassAdapter
{
    public static class FileMaskMatcher
    {
        public static bool IsMatch(string fileName, string mask)
        {
            if (string.IsNullOrEmpty(mask) || mask == "*")
                return true;

            var regex = "^" + Regex.Escape(mask)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            return Regex.IsMatch(Path.GetFileName(fileName), regex, RegexOptions.IgnoreCase);
        }
    }
}
