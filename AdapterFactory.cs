using System;

namespace EaiClassAdapter
{
    public static class AdapterFactory
    {
        public static ITransferAdapter Create(AdapterType type)
        {
            switch (type)
            {
                case AdapterType.File:
                    return new FileAdapter();

                case AdapterType.FTP:
                    return new FTPAdapter();

                case AdapterType.SFTP:
                    return new SFTPAdapter();

                case AdapterType.HTTP:
                    throw new NotImplementedException("HTTP Adapter 尚未實作");

                case AdapterType.SQL:
                    throw new NotImplementedException("SQL Adapter 尚未實作");

                default:
                    throw new ArgumentException($"未支援的 AdapterType: {type}");
            }
        }

        /// <summary>
        /// 加強版：能處理帶引號、空白、< > 等髒資料的路徑
        /// </summary>
        public static ITransferAdapter CreateByPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return new FileAdapter();

            // 先做最終清理（防萬一）
            string clean = path.Trim().Trim('\'', '"', '<', '>');

            // 使用 Uri 判斷（最準）
            if (Uri.TryCreate(clean, UriKind.Absolute, out Uri uri))
            {
                string scheme = uri.Scheme.ToLowerInvariant();

                if (scheme == "ftp")
                    return new FTPAdapter();

                if (scheme == "sftp")
                    return new SFTPAdapter();
            }

            // 後備判斷（萬一 Uri 失敗）
            string lower = clean.ToLowerInvariant();
            if (lower.StartsWith("ftp://"))
                return new FTPAdapter();

            if (lower.StartsWith("sftp://"))
                return new SFTPAdapter();

            return new FileAdapter();
        }

        public static AdapterType ResolveByPath(string path)
        {
            string cleanPath = path?.Trim().Trim('\'', '"', '<', '>') ?? string.Empty;

            if (Uri.TryCreate(cleanPath, UriKind.Absolute, out Uri uri))
            {
                string scheme = uri.Scheme.ToLowerInvariant();
                if (scheme == "ftp") return AdapterType.FTP;
                if (scheme == "sftp") return AdapterType.SFTP;
            }

            if (cleanPath.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                return AdapterType.FTP;

            if (cleanPath.StartsWith("sftp://", StringComparison.OrdinalIgnoreCase))
                return AdapterType.SFTP;

            return AdapterType.File;
        }
    }
}