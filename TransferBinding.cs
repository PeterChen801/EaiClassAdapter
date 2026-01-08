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

            // ====== 建立 Adapter ======
            var sourceAdapter = AdapterFactory.CreateByPath(sourcePath);
            var destAdapter = AdapterFactory.CreateByPath(destPath);

            //Console.WriteLine($"[Transfer] Source Adapter: {sourceAdapter.GetType().Name}");
            //Console.WriteLine($"[Transfer] Dest Adapter:   {destAdapter.GetType().Name}");

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

            //Console.WriteLine($"[Transfer] Received {files.Length} files");
            EaiComponent.WriteEventLog_Inf("EaiClassAdapter", $"[Transfer] Received {files.Length} files");

            // ★ 傳遞 JobTempFolder 給 SendContext，用於完成後自動刪除
            sc.JobTempFolder = rc.JobTempFolder;

            foreach (var file in files)
            {
                //Console.WriteLine($"[Transfer] Sending: {Path.GetFileName(file)}");
                EaiComponent.WriteEventLog_Inf("EaiClassAdapter", $"[Transfer] Sending: {Path.GetFileName(file)}");
                destAdapter.Send(file, sc);
            }

            // ====== Send 完後自動刪除 Job 專用 Temp Folder ======
            if (!string.IsNullOrEmpty(sc.JobTempFolder) && Directory.Exists(sc.JobTempFolder))
            {
                try
                {
                    Directory.Delete(sc.JobTempFolder, true);
                    EaiComponent.WriteEventLog_Inf("EaiClassAdapter",
                        $"Job Temp Folder 已刪除: {sc.JobTempFolder}");
                    sc.JobTempFolder = null;
                }
                catch (Exception ex)
                {
                    EaiComponent.WriteEventLog_Inf(
                        "EaiClassAdapter",
                        $"刪除 Job Temp Folder 失敗: {sc.JobTempFolder}\r\n錯誤: {ex.Message}"
                    );
                }
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
            rc.User = Utils.NormalizeString(user);
            rc.Password = pwd?.Trim('\'', '"') ?? string.Empty;
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
            sc.SendFileNameFormat = string.IsNullOrWhiteSpace(fileNameFormat)
                ? "%SourceFileName%"
                : fileNameFormat;

            sc.User = Utils.NormalizeString(user);
            sc.Password = pwd?.Trim('\'', '"') ?? string.Empty;
            sc.NetworkRetry = retry;
            sc.NetworkRetryInterval = interval;
            sc.FileCopyMode = string.IsNullOrWhiteSpace(copyMode)
                ? "OVERWRITE"
                : copyMode.Trim().ToUpperInvariant();

            string cleanPath = path?.Trim().Trim('\'', '"', '<', '>') ?? string.Empty;
            cleanPath = cleanPath.Replace('\\', '/');

            if (adapter is FTPAdapter || adapter is SFTPAdapter)
            {
                if (Uri.TryCreate(cleanPath, UriKind.Absolute, out Uri uri))
                {
                    sc.Host = uri.Host;
                    sc.SendPath = uri.AbsolutePath.TrimStart('/');
                    if (string.IsNullOrEmpty(sc.SendPath)) sc.SendPath = "/";
                }
                else
                {
                    sc.Host = cleanPath;
                    sc.SendPath = "/";
                }
            }
            else
            {
                sc.SendPath = cleanPath;
                if (sc.SendPath.Length > 3 && sc.SendPath.EndsWith("\\")) sc.SendPath = sc.SendPath.TrimEnd('\\');
                sc.Host = null;
            }
        }
    }
}
