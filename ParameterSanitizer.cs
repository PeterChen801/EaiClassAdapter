using System;

namespace EaiClassAdapter
{
    public static class ParameterSanitizer
    {
        public static TransferParameters Sanitize(TransferParameters p)
        {
            if (string.IsNullOrEmpty(p.SourceMask))
                p.SourceMask = "*.*";
            if (string.IsNullOrEmpty(p.DestFileNameFormat))
                p.DestFileNameFormat = "%SourceFileName%";
            return p;
        }
    }
}
