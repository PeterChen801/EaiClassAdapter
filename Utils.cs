using System;
using System.IO;
using System.Text.RegularExpressions;

namespace EaiClassAdapter
{
    public static class Utils
    {

        private static bool NormalizeBoolean(string value, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            value = value.Trim().ToUpperInvariant();

            switch (value)
            {
                case "Y":
                case "YES":
                case "TRUE":
                case "1":
                    return true;

                case "N":
                case "NO":
                case "FALSE":
                case "0":
                    return false;

                default:
                    return defaultValue;
            }
        }

        public static bool ToBool(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return value.Trim().ToUpperInvariant() == "Y" || value.Trim() == "1";
        }

        public static int ToInt(string value, int defaultValue = 0)
        {
            int result;
            return int.TryParse(value, out result) ? result : defaultValue;
        }


        // Mask match: *.txt / abc*.csv
        public static bool IsMatch(string fileName, string mask)
        {
            if (string.IsNullOrEmpty(mask) || mask == "*.*")
                return true;

            string pattern = "^" + Regex.Escape(mask)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            return Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase);
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            string result = path.Trim();

            // 先去掉所有外層引號（單引號、雙引號）
            while ((result.StartsWith("'") && result.EndsWith("'")) ||
                   (result.StartsWith("\"") && result.EndsWith("\"")))
            {
                result = result.Substring(1, result.Length - 2).Trim();
            }

            // 重要！如果是 ftp:// 或 sftp:// 開頭，直接回傳，不做任何斜線處理
            string lower = result.ToLowerInvariant();
            if (lower.StartsWith("ftp://") || lower.StartsWith("sftp://") ||
                lower.StartsWith("http://") || lower.StartsWith("https://"))
            {
                // 只去掉結尾多餘的斜線（如果有）
                result = result.TrimEnd('/', '\\');
                return result;
            }

            // 以下才是本地路徑才處理的邏輯
            result = result.Replace('/', '\\');
            if (result.Length > 3 && result.EndsWith("\\"))
            {
                result = result.TrimEnd('\\');
            }

            return result;
        }

        public static string NormalizeString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim();

            if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (value.StartsWith("'") && value.EndsWith("'"))
                value = value.Substring(1, value.Length - 2);

            return value.Trim();
        }


        public static string FormatFileName(string format, string sourceFileName)
        {
            string fileName = format;
            if (fileName.Contains("%SourceFileName%"))
                fileName = fileName.Replace("%SourceFileName%", sourceFileName);

            DateTime now = DateTime.Now;
            while (fileName.Contains("%"))
            {
                int start = fileName.IndexOf("%");
                int end = fileName.IndexOf("%", start + 1);
                if (end <= start) break;

                string token = fileName.Substring(start + 1, end - start - 1);
                string value = string.Empty;

                try
                {
                    value = now.ToString(token);
                }
                catch { value = ""; }

                fileName = fileName.Substring(0, start) + value + fileName.Substring(end + 1);
            }

            return fileName;
        }

        public static void Log(string enableLog, string message)
        {
            if (!ToBool(enableLog)) return;            
            EaiComponent.WriteEventLog_Inf("EaiClassAdapter", "Log:\r\n"+ "{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
        }
    }
}
