namespace EaiClassAdapter
{
    public class SendContext
    {
        // 目的端
        public string SendPath { get; set; }
        public string SendFileName { get; set; }

        public string Protocol { get; set; }

        // 帳密
        public string User { get; set; }
        public string Password { get; set; }

        // === Remote ===
        public string Host { get; set; }
        public int Port { get; set; } = 22;

        // FTP only
        public string FtpAddress { get; set; }

        // SFTP 用
        public string PrivateKeyPath { get; set; }
        // Retry
        public int NetworkRetry { get; set; }
        public int NetworkRetryInterval { get; set; }

        // File Send only

        // === File ===
        // e.g. "{yyyyMMddHHmmss}_{OriginalName}"
        public string SendFileNameFormat { get; set; } = "{OriginalName}";

        public string FileCopyMode { get; set; } //可改成 Appen/CreteNew/Overwrite
        
        // 新增：Job 專用 temp 資料夾
        public string JobTempFolder { get; set; }
    }
}

