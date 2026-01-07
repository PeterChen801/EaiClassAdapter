using System;

namespace EaiClassAdapter
{
    public class TransferParameters
    {
        public string SourcePath { get; set; }
        public string SourceMask { get; set; }
        public string SourceUser { get; set; }
        public string SourcePassword { get; set; }
        public int SourceRetryCount { get; set; }
        public int SourceRetryInterval { get; set; }

        public string DestPath { get; set; }
        public string DestFileNameFormat { get; set; }
        public string DestUser { get; set; }
        public string DestPassword { get; set; }
        public int DestRetryCount { get; set; }
        public int DestRetryInterval { get; set; }

        public string FileCopyMode { get; set; } // APPEND, OVERWRITE, CREATENEW
        public bool MoveAfterCopy { get; set; }
        public bool EnableLog { get; set; }

        // 方便轉 Y/N 給方法使用
        public string MoveAfterCopyFlag => MoveAfterCopy ? "Y" : "N";
        public string EnableLogFlag => EnableLog ? "Y" : "N";
    }
}
