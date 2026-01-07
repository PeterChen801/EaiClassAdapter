using System;
using System.Text.RegularExpressions;
using System.IO;

namespace EaiClassAdapter
{
    public static class FileNameFormatter
    {
        public static string Format(string format, string sourceFileName)
        {
            if (string.IsNullOrEmpty(format))
                return Path.GetFileName(sourceFileName);

            string result = format;

            // 先處理來源檔名（避免與日期 token 衝突）
            result = Regex.Replace(result, @"%SourceFileName%",
                m => Path.GetFileName(sourceFileName),
                RegexOptions.IgnoreCase);

            DateTime now = DateTime.Now;

            // 定義日期格式對應表（key: token pattern, value: .NET 格式字串）
            var datePatterns = new[]
            {
                (Pattern: @"%yyyyMMdd%",          Format: "yyyyMMdd"),
                (Pattern: @"%yyyymmdd%",          Format: "yyyyMMdd"),
                (Pattern: @"%yyyyMMddHHmmss%",    Format: "yyyyMMddHHmmss"),
                (Pattern: @"%yyyymmddhhMMss%",    Format: "yyyyMMddHHmmss"),
                (Pattern: @"%yyyyMMdd_HHmmss%",   Format: "yyyyMMdd_HHmmss"),
                (Pattern: @"%yyyy-MM-dd%",        Format: "yyyy-MM-dd"),
                (Pattern: @"%yyyy-MM-dd_HH-mm-ss%", Format: "yyyy-MM-dd_HH-mm-ss"),
                (Pattern: @"%HHmmss%",            Format: "HHmmss"),
                (Pattern: @"%hhmmss%",            Format: "HHmmss"),
                (Pattern: @"%yyyy%",              Format: "yyyy"),
                (Pattern: @"%MM%",                Format: "MM"),
                (Pattern: @"%dd%",                Format: "dd"),
                (Pattern: @"%HH%",                Format: "HH"),
                (Pattern: @"%mm%",                Format: "mm"),
                (Pattern: @"%ss%",                Format: "ss")
                // 可繼續擴充更多你需要的格式
            };

            // 使用 Regex 一次處理所有日期相關的 token
            foreach (var (pattern, dateFormat) in datePatterns)
            {
                result = Regex.Replace(result, pattern,
                    m => now.ToString(dateFormat),
                    RegexOptions.IgnoreCase);
            }

            return result;
        }
    }
}