namespace EaiClassAdapter
{
    public class ReceiveContext
    {
        // 共用
        public string ReceivePath { get; set; }
        public string ReceiveFileMask { get; set; }

        public int NetworkRetry { get; set; }
        public int NetworkRetryInterval { get; set; }

        // === Remote ===
        public string Host { get; set; }
        public int Port { get; set; } = 22;

        // File / FTP 帳密
        public string User { get; set; }
        public string Password { get; set; }

        // FTP only
        public string FtpAddress { get; set; }

        // === Local ===
        public string LocalTempPath { get; set; }
        // 行為
        public bool DeleteAfterReceive { get; set; }
    }
}
