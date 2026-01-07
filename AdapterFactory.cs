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

        public static ITransferAdapter CreateByPath(string path)
        {
            if (path.StartsWith("sftp://", StringComparison.OrdinalIgnoreCase))
                return new SFTPAdapter();

            if (path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                return new FTPAdapter();

            return new FileAdapter();
        }

        public static AdapterType ResolveByPath(string path)
        {
            if (path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                return AdapterType.FTP;
            if (path.StartsWith("sftp://", StringComparison.OrdinalIgnoreCase))
                return AdapterType.SFTP;

            return AdapterType.File;
        }

    }
}
