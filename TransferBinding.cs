using System;
using System.IO;

namespace EaiClassAdapter
{
    public class TransferBinding
    {
        public void Execute(
            string sourcePath,
            string sourceMask,
            string sourceUser,
            string sourcePwd,
            string srcRetry,
            string srcInterval,
            string destPath,
            string destFileNameFormat,
            string destUser,
            string destPwd,
            string destRetry,
            string destInterval,
            string fileCopymode,
            string fileDeleteAfterReceive)
        {
            // ====== 參數預處理（加強清理） ======
            sourcePath = CleanPath(sourcePath);
            destPath = CleanPath(destPath);
            sourceMask = Utils.NormalizeString(sourceMask ?? "*.*");

            int srcRetryValue = Utils.ToInt(srcRetry);
            int srcIntervalValue = Utils.ToInt(srcInterval);
            int destRetryValue = Utils.ToInt(destRetry);
            int destIntervalValue = Utils.ToInt(destInterval);
            bool deleteAfter = Utils.ToBool(fileDeleteAfterReceive);

            // Debug log - 建議保留到測試完成
            Console.WriteLine($"[Transfer] Source: {sourcePath}");
            Console.WriteLine($"[Transfer] Dest:   {destPath}");
            Console.WriteLine($"[Transfer] Mask:   {sourceMask}");

            // ====== 建立 Adapter ======
            var sourceAdapter = AdapterFactory.CreateByPath(sourcePath);
            var destAdapter = AdapterFactory.CreateByPath(destPath);

            Console.WriteLine($"[Transfer] Source Adapter: {sourceAdapter.GetType().Name}");
            Console.WriteLine($"[Transfer] Dest Adapter:   {destAdapter.GetType().Name}");

            // ====== 建立 Context ======
            var rc = new ReceiveContext();
            var sc = new SendContext();

            // Source Context
            SetupReceiveContext(rc, sourceAdapter, sourcePath, sourceMask,
                                sourceUser, sourcePwd, srcRetryValue, srcIntervalValue, deleteAfter);

            // Destination Context
            SetupSendContext(sc, destAdapter, destPath, destFileNameFormat,
                             destUser, destPwd, destRetryValue, destIntervalValue, fileCopymode);

            // ====== 執行傳輸 ======
            string[] files = sourceAdapter.Receive(rc);

            Console.WriteLine($"[Transfer] Received {files.Length} files");

            foreach (var file in files)
            {
                Console.WriteLine($"[Transfer] Sending: {Path.GetFileName(file)}");
                destAdapter.Send(file, sc);
            }
        }

        // 強力路徑清理（處理引號、< > 等髒資料）
        private static string CleanPath(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string result = input.Trim().Trim('\'', '"', '<', '>');

            // 處理常見的 '<ftp://...' 這種意外格式
            if (result.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) == false &&
                result.StartsWith("sftp://", StringComparison.OrdinalIgnoreCase) == false)
            {
                result = result.TrimStart('<');
            }

            return Utils.NormalizePath(result);
        }

        private void SetupReceiveContext(ReceiveContext rc, ITransferAdapter adapter,
            string path, string mask, string user, string pwd, int retry, int interval, bool deleteAfter)
        {
            rc.ReceiveFileMask = mask;
            rc.User = user;
            rc.Password = pwd;
            rc.NetworkRetry = retry;
            rc.NetworkRetryInterval = interval;
            rc.DeleteAfterReceive = deleteAfter;
            rc.LocalTempPath = Path.GetTempPath();

            if (adapter is FTPAdapter || adapter is SFTPAdapter)
            {
                if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
                {
                    rc.Host = uri.Host;
                    rc.ReceivePath = uri.AbsolutePath.TrimStart('/');
                }
                else
                {
                    rc.Host = path;
                    rc.ReceivePath = "/";
                }
            }
            else
            {
                rc.ReceivePath = path;
            }
        }

        private void SetupSendContext(SendContext sc, ITransferAdapter adapter,
    string path, string fileNameFormat, string user, string pwd,
    int retry, int interval, string copyMode)
        {
            // 1. 基本屬性設定
            sc.SendFileNameFormat = string.IsNullOrWhiteSpace(fileNameFormat)
                ? "%SourceFileName%"
                : fileNameFormat;

            sc.User = Utils.NormalizeString(user);
            sc.Password = pwd;  // 密碼不做 Normalize，避免影響特殊字元
            sc.NetworkRetry = retry;
            sc.NetworkRetryInterval = interval;

            // 2. 檔案複製模式預設值與標準化
            sc.FileCopyMode = string.IsNullOrWhiteSpace(copyMode)
                ? "OVERWRITE"
                : copyMode.Trim().ToUpperInvariant();

            // 3. 處理目的地路徑（核心部分）
            string cleanPath = path?.Trim().Trim('\'', '"', '<', '>') ?? string.Empty;

            // 強制把反斜線轉成正斜線（避免 ftp:\\ 這種常見錯誤）
            cleanPath = cleanPath.Replace('\\', '/');

            if (adapter is FTPAdapter || adapter is SFTPAdapter)
            {
                // 遠端協議：使用 Uri 解析
                if (Uri.TryCreate(cleanPath, UriKind.Absolute, out Uri uri))
                {
                    sc.Host = uri.Host;

                    // 取得路徑部分，去掉開頭的斜線（FTP/SFTP 通常不需要開頭 /）
                    sc.SendPath = uri.AbsolutePath.TrimStart('/');

                    // 如果路徑為空，預設根目錄
                    if (string.IsNullOrEmpty(sc.SendPath))
                    {
                        sc.SendPath = "/";
                    }
                }
                else
                {
                    // Uri 解析失敗的 fallback（例如使用者只給 host 名稱）
                    sc.Host = cleanPath;  // 假設整個是 host
                    sc.SendPath = "/";    // 預設根目錄
                }
            }
            else
            {
                // 本地檔案系統：直接使用清理後的路徑
                sc.SendPath = cleanPath;

                // 確保本地路徑結尾沒有多餘斜線（可選，視需求保留或移除）
                if (sc.SendPath.Length > 3 && sc.SendPath.EndsWith("\\"))
                {
                    sc.SendPath = sc.SendPath.TrimEnd('\\');
                }

                // 本地路徑不需要 Host
                sc.Host = null;
            }
            
            // 可選：加入 debug log（測試階段使用，上線可註解）
            // Console.WriteLine($"[SetupSendContext] Adapter: {adapter.GetType().Name}");
            // Console.WriteLine($"[SetupSendContext] Host: {sc.Host ?? "N/A"}");
            // Console.WriteLine($"[SetupSendContext] SendPath: {sc.SendPath}");
            // Console.WriteLine($"[SetupSendContext] FileNameFormat: {sc.SendFileNameFormat}");
        }
    }
}