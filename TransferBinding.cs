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
            // ====== 參數預處理 ======
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

            // ====== 建立 Context ======
            var rc = new ReceiveContext();
            var sc = new SendContext();

            // Source
            SetupReceiveContext(
                rc,
                sourceAdapter,
                sourcePath,
                sourceMask,
                sourceUser,
                sourcePwd,
                srcRetryValue,
                srcIntervalValue,
                deleteAfter);

            // Destination
            SetupSendContext(
                sc,
                destAdapter,
                destPath,
                destFileNameFormat,
                destUser,
                destPwd,
                destRetryValue,
                destIntervalValue,
                fileCopymode);

            // ====== 執行 Receive ======
            string[] files = sourceAdapter.Receive(rc);

            EaiComponent.WriteEventLog_Inf(
                "EaiClassAdapter",
                $"[Transfer] Received {files.Length} files");

            // 傳遞 JobTempFolder
            sc.JobTempFolder = rc.JobTempFolder;

            foreach (var file in files)
            {
                EaiComponent.WriteEventLog_Inf(
                    "EaiClassAdapter",
                    $"[Transfer] Sending: {Path.GetFileName(file)}");

                destAdapter.Send(file, sc);
            }

            // ====== 清除 Job Temp Folder ======
            if (!string.IsNullOrEmpty(sc.JobTempFolder) && Directory.Exists(sc.JobTempFolder))
            {
                try
                {
                    Directory.Delete(sc.JobTempFolder, true);
                    EaiComponent.WriteEventLog_Inf(
                        "EaiClassAdapter",
                        $"Job Temp Folder 已刪除: {sc.JobTempFolder}");
                }
                catch (Exception ex)
                {
                    EaiComponent.WriteEventLog_Inf(
                        "EaiClassAdapter",
                        $"刪除 Job Temp Folder 失敗: {sc.JobTempFolder}\r\n錯誤: {ex.Message}");
                }
            }
        }

        // =========================
        // Path Clean
        // =========================
        private static string CleanPath(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string result = input.Trim().Trim('\'', '"', '<', '>');

            if (!result.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) &&
                !result.StartsWith("sftp://", StringComparison.OrdinalIgnoreCase))
            {
                result = result.TrimStart('<');
            }

            return Utils.NormalizePath(result);
        }

        // =========================
        // Receive Context
        // =========================
        private void SetupReceiveContext(
            ReceiveContext rc,
            ITransferAdapter adapter,
            string path,
            string mask,
            string user,
            string pwd,
            int retry,
            int interval,
            bool deleteAfter)
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
                Uri uri = BuildUri(path, adapter is SFTPAdapter);

                rc.Protocol = uri.Scheme.ToUpperInvariant();
                rc.Host = uri.Host;
                rc.Port = uri.Port;

                // ⭐ 關鍵修正：SFTP 路徑必須保留 '/'
                string absPath = uri.AbsolutePath;
                if (string.IsNullOrEmpty(absPath) || absPath == "/")
                    rc.ReceivePath = "/";
                else
                    rc.ReceivePath = absPath;

            }
            else
            {
                rc.ReceivePath = path;
            }

            rc.PrivateKeyPath = ExtractPrivateKeyPath(pwd);
        }

        // =========================
        // Send Context
        // =========================
        private void SetupSendContext(
            SendContext sc,
            ITransferAdapter adapter,
            string path,
            string fileNameFormat,
            string user,
            string pwd,
            int retry,
            int interval,
            string copyMode)
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

            if (adapter is FTPAdapter || adapter is SFTPAdapter)
            {
                Uri uri = BuildUri(path, adapter is SFTPAdapter);

                sc.Protocol = uri.Scheme.ToUpperInvariant();
                sc.Host = uri.Host;
                sc.Port = uri.Port;

                string absPath = uri.AbsolutePath;
                if (string.IsNullOrEmpty(absPath) || absPath == "/")
                    sc.SendPath = "/";
                else
                    sc.SendPath = absPath;
            }
            else
            {
                sc.SendPath = path;
                sc.Host = null;
            }

            sc.PrivateKeyPath = ExtractPrivateKeyPath(pwd);
        }

        // =========================
        // Helper
        // =========================
        private Uri BuildUri(string path, bool isSftp)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
                return uri;

            string scheme = isSftp ? "sftp://" : "ftp://";
            return new Uri($"{scheme}{path}");
        }

        /// <summary>
        /// 支援 Password | PrivateKey | PrivateKey + Passphrase
        /// </summary>
        private string ExtractPrivateKeyPath(string pwd)
        {
            if (string.IsNullOrWhiteSpace(pwd)) return null;

            string clean = pwd.Trim('\'', '"');
            return File.Exists(clean) ? clean : null;
        }
    }
}
