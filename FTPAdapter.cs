using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace EaiClassAdapter
{
    public class FTPAdapter : ITransferAdapter
    {
        // =========================
        // ListFiles
        // =========================
        public string[] ListFiles(string path, string mask, string user, string password)
        {
            var files = new List<string>();
            string listUri = path.TrimEnd('/');

            var request = (FtpWebRequest)WebRequest.Create(listUri);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(user, password);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;

            using (var response = (FtpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var file = reader.ReadLine();
                    if (FileMaskMatcher.IsMatch(file, mask))
                        files.Add(file);
                }
            }

            return files.ToArray();
        }

        // =========================
        // Receive (FTP → Temp)
        // =========================
        public string[] Receive(ReceiveContext context)
        {
            var downloadedFiles = new List<string>();

            string host = (context.Host ?? "localhost").Trim();
            string receivePath = (context.ReceivePath ?? "/").Trim('/');
            string listUri = $"ftp://{host}/{receivePath}";

            var listRequest = CreateRequest(
                listUri,
                WebRequestMethods.Ftp.ListDirectory,
                context.User,
                context.Password);

            string jobTempDir = Path.Combine(Path.GetTempPath(), "EaiJobTemp", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(jobTempDir);
            context.JobTempFolder = jobTempDir; // 給 TransferBinding 統一刪除

            using (var listResponse = (FtpWebResponse)listRequest.GetResponse())
            using (var listStream = listResponse.GetResponseStream())
            using (var reader = new StreamReader(listStream))
            {
                while (!reader.EndOfStream)
                {
                    string fileName = reader.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(fileName)) continue;
                    if (!FileMaskMatcher.IsMatch(fileName, context.ReceiveFileMask)) continue;

                    string remoteFile = $"{listUri}/{fileName}";
                    string localTempFile = Path.Combine(jobTempDir, fileName);

                    DownloadFile(remoteFile, localTempFile, context);
                    downloadedFiles.Add(localTempFile);

                    if (context.DeleteAfterReceive)
                        Delete(remoteFile);
                }
            }

            return downloadedFiles.ToArray();
        }

        // =========================
        // Send (Temp → FTP)
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
            string sendPath = (context.SendPath ?? "/").Trim('/');

            string remotePath = string.IsNullOrEmpty(sendPath)
                ? $"/{finalFileName}"
                : $"/{sendPath}/{finalFileName}";

            string uploadUri = $"ftp://{host}{remotePath}";

            var request = CreateRequest(uploadUri, WebRequestMethods.Ftp.UploadFile, context.User, context.Password);

            using (var fs = File.OpenRead(localFilePath))
            using (var reqStream = request.GetRequestStream())
            {
                fs.CopyTo(reqStream);
            }

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                EaiComponent.WriteEventLog_Inf(
                    "EaiClassAdapter",
                    $"FTP 上傳成功\r\n目的檔案: {uploadUri}\r\n狀態: {response.StatusDescription}"
                );
            }
        }

        // =========================
        // Delete
        // =========================
        public void Delete(string remotePath)
        {
            var request = CreateRequest(remotePath, WebRequestMethods.Ftp.DeleteFile, null, null);

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                EaiComponent.WriteEventLog_Inf(
                    "EaiClassAdapter",
                    $"FTP 刪除成功: {response.StatusDescription}"
                );
            }
        }

        // =========================
        // Helper
        // =========================
        private void DownloadFile(string remoteFile, string localFile, ReceiveContext context)
        {
            var request = CreateRequest(remoteFile, WebRequestMethods.Ftp.DownloadFile, context.User, context.Password);

            using (var response = (FtpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var fs = new FileStream(localFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.CopyTo(fs);
            }
        }

        private FtpWebRequest CreateRequest(string uri, string method, string user, string password)
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = method;

            if (!string.IsNullOrEmpty(user))
                request.Credentials = new NetworkCredential(user, password);

            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            return request;
        }
    }
}
