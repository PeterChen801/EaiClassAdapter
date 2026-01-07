using System;

namespace EaiClassAdapter
{
    public class FileTransferException : Exception
    {
        public string ErrorCode { get; }

        public FileTransferException(string code, string message, Exception inner = null)
            : base(message, inner)
        {
            ErrorCode = code;
        }
    }
}
