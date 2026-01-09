using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace EaiClassAdapter
{
    public class SFTPAdapter : ITransferAdapter
    {
        // =========================
        // ListFiles
        // =========================
        public string[] ListFiles(string path, string mask, string user, string password)
        {
            var files = new List<string>();

            using (var client = CreateClientFromUri(path, user, password, null))
            {
                client.Connect();

                client.ChangeDirectory(client.WorkingDirectory);

                foreach (var file in client.ListDirectory("."))
                {
                    if (file.IsRegularFile && FileMaskMatcher.IsMatch(file.Name, mask))
                        files.Add(file.Name);
                }

                client.Disconnect();
            }

            return files.ToArray();
        }

        // =========================
        // Receive (SFTP → Temp)
        // =========================
        public string[] Receive(ReceiveContext context)
        {
            var downloadedFiles = new List<string>();

            string host = (context.Host ?? "localhost").Trim();
            int port = context.Port > 0 ? context.Port : 22;

            // ⭐ 關鍵：保證為絕對路徑
            string receivePath = context.ReceivePath;
            if (string.IsNullOrWhiteSpace(receivePath))
                receivePath = "/";
            if (!receivePath.StartsWith("/"))
                receivePath = "/" + receivePath;

            string jobTempDir = Path.Combine(
                Path.GetTempPath(),
                "EaiJobTemp",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(jobTempDir);
            context.JobTempFolder = jobTempDir;

            using (var client = CreateClient(
                host,
                port,
                context.User,
                context.Password,
                context.PrivateKeyPath))
            {
                client.Connect();

                // ⭐ 關鍵：一定要先 ChangeDirectory
                client.ChangeDirectory(receivePath);

                //EaiComponent.WriteEventLog_Inf(
                //    "EaiClassAdapter",
                //    $"[SFTP] ListDirectory Path = {receivePath}");

                foreach (var file in client.ListDirectory("."))
                {
                    //EaiComponent.WriteEventLog_Inf(
                    //    "EaiClassAdapter",
                    //    $"[SFTP] Found entry: {file.Name} (IsFile={file.IsRegularFile})");

                    //if (!file.IsRegularFile) continue;  這樣寫法太嚴格，造成檔案忽略
                    if (file.IsDirectory) continue;

                    if (!FileMaskMatcher.IsMatch(file.Name, context.ReceiveFileMask)) continue;

                    string localTempFile = Path.Combine(jobTempDir, file.Name);

                    using (var fs = new FileStream(localTempFile, FileMode.Create, FileAccess.Write))
                    {
                        client.DownloadFile(file.Name, fs);
                    }

                    downloadedFiles.Add(localTempFile);

                    if (context.DeleteAfterReceive)
                        client.DeleteFile(file.Name);
                }

                client.Disconnect();
            }

            return downloadedFiles.ToArray();
        }

        // =========================
        // Send (Temp → SFTP)
        // =========================
        public void Send(string localFilePath, SendContext context)
        {
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException($"來源檔案不存在: {localFilePath}");

            string originalFileName = Path.GetFileName(localFilePath);

            string finalFileName = FileNameFormatter.Format(
                context.SendFileNameFormat ?? "%SourceFileName%",
                originalFileName
            ).Trim('\'');

            string host = (context.Host ?? "localhost").Trim();
            int port = context.Port > 0 ? context.Port : 22;

            string sendPath = context.SendPath;
            if (string.IsNullOrWhiteSpace(sendPath))
                sendPath = "/";
            if (!sendPath.StartsWith("/"))
                sendPath = "/" + sendPath;

            string remoteFile = $"{sendPath}/{finalFileName}";

            using (var client = CreateClient(
                host,
                port,
                context.User,
                context.Password,
                context.PrivateKeyPath))
            {
                client.Connect();

                client.ChangeDirectory(sendPath);

                using (var fs = File.OpenRead(localFilePath))
                {
                    client.UploadFile(fs, finalFileName, true);
                }

                client.Disconnect();
            }

            EaiComponent.WriteEventLog_Inf(
                "EaiClassAdapter",
                $"SFTP 上傳成功\r\n目的檔案: {remoteFile}");
        }

        // =========================
        // Delete
        // =========================
        public void Delete(string remotePath)
        {
            throw new NotSupportedException(
                "SFTPAdapter.Delete 需 Host Context，請由 Receive 控制刪除");
        }

        // =========================
        // Helper
        // =========================
        private SftpClient CreateClient(
            string host,
            int port,
            string user,
            string password,
            string privateKeyPath)
        {
            var authMethods = new List<AuthenticationMethod>();

            if (!string.IsNullOrEmpty(privateKeyPath))
            {
                var keyFile = string.IsNullOrEmpty(password)
                    ? new PrivateKeyFile(privateKeyPath)
                    : new PrivateKeyFile(privateKeyPath, password);

                authMethods.Add(new PrivateKeyAuthenticationMethod(user, keyFile));
            }

            if (!string.IsNullOrEmpty(password))
            {
                authMethods.Add(new PasswordAuthenticationMethod(user, password));
            }

            var connectionInfo = new ConnectionInfo(
                host,
                port,
                user,
                authMethods.ToArray());

            return new SftpClient(connectionInfo);
        }

        private SftpClient CreateClientFromUri(
            string uriString,
            string user,
            string password,
            string privateKeyPath)
        {
            var uri = new Uri(
                uriString.StartsWith("sftp://")
                ? uriString
                : $"sftp://{uriString}");

            return CreateClient(
                uri.Host,
                uri.Port > 0 ? uri.Port : 22,
                user,
                password,
                privateKeyPath);
        }
    }
}
