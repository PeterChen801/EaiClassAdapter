using System;
using System.IO;

namespace EaiClassAdapter
{
    public class TransferFile
    {
        public string FullPath { get; set; }
        public string FileName { get; set; }
        public long Size { get; set; }

        public TransferFile(string fullPath)
        {
            FullPath = fullPath;
            FileName = Path.GetFileName(fullPath);
            Size = new FileInfo(fullPath).Length;
        }
    }
}
