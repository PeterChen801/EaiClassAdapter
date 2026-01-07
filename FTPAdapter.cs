using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace EaiClassAdapter
{
    public class FTPAdapter : ITransferAdapter
    {
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
                    {
                        files.Add(file);
                    }
                }
            }

            return files.ToArray();
        }

        // =========================
        // Receive (Download)
        // =========================
        public string[] Receive(ReceiveContext context)
        {
            List<string> downloadedFiles = new List<string>();

            string host = (context.Host ?? "localhost").Trim();
            string receivePath = (context.ReceivePath ?? "/").Trim('/');
            string listUri = $"ftp://{host}/{receivePath}";
          
            FtpWebRequest listRequest = CreateRequest(
                listUri,
                WebRequestMethods.Ftp.ListDirectory,
                context.User,
                context.Password);

            EaiComponent.WriteEventLog_Inf("EaiClassAdapter", $"Ftp Receive\r\n來源路徑: {listUri}\r\nftpUser: {context.User}\r\nftpPassword: {context.Password}");


            using (var listResponse = (FtpWebResponse)listRequest.GetResponse())
            using (var listStream = listResponse.GetResponseStream())
            using (var reader = new StreamReader(listStream))
            {
                while (!reader.EndOfStream)
                {
                    string fileName = reader.ReadLine().Trim();
                    if (!FileMaskMatcher.IsMatch(fileName, context.ReceiveFileMask))
                        continue;

                    string remoteFile = $"{listUri}/{fileName}";
                    string localFile = Path.Combine(context.LocalTempPath, fileName);

                    DownloadFile(remoteFile, localFile, context);
                    downloadedFiles.Add(localFile);

                    if (context.DeleteAfterReceive)
                    {
                        Delete(remoteFile);
                    }
                }
            }

            return downloadedFiles.ToArray();
        }

        // =========================
        // Send (Upload)
        // =========================
        public void Send(string localFilePath, SendContext context)
        {
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException($"來源檔案不存在: {localFilePath}");

            // 取得乾淨原始檔名
            string originalFileName = Path.GetFileName(localFilePath).Trim('\'');

            // 產生目的檔名
            string fileName = FileNameFormatter.Format(
                context.SendFileNameFormat ?? "%SourceFileName%",
                originalFileName
            );

            // 最終清理檔名（防單引號）
            fileName = fileName.Trim('\'');

            // 清理 Host 與 SendPath
            string host = (context.Host ?? "localhost").Trim();
            string sendPath = (context.SendPath ?? "/").Trim('/');

            // 組裝遠端路徑
            string remotePath = string.IsNullOrEmpty(sendPath)
                ? $"/{fileName}"
                : $"/{sendPath}/{fileName}";

            // 完整 URI
            string uploadUri = $"ftp://{host}{remotePath}";

            // 日誌
            EaiComponent.WriteEventLog_Inf("EaiClassAdapter",
                $"Ftp Send\r\n目的路徑: {uploadUri}\r\n來源檔案: {localFilePath}\r\nftpUser: {context.User}\r\nftpPassword: {context.Password}");

            // 建立請求
            FtpWebRequest request = CreateRequest(
                uploadUri,
                WebRequestMethods.Ftp.UploadFile,
                context.User,
                context.Password);

            // 上傳
            using (var fs = File.OpenRead(localFilePath))
            using (var reqStream = request.GetRequestStream())
            {
                fs.CopyTo(reqStream);
            }

            // 關鍵：讀取 Response 以完成上傳
            try
            {
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    string status = response.StatusDescription.Trim();
                    EaiComponent.WriteEventLog_Inf("EaiClassAdapter",
                        $"FTP 上傳成功\r\n目的路徑: {uploadUri}\r\n伺服器回應: {status}");
                }
            }
            catch (WebException ex)
            {
                string err = ex.Message;
                if (ex.Response is FtpWebResponse resp)
                    err += $"\r\n伺服器回應: {resp.StatusDescription}";

                EaiComponent.WriteEventLog_Inf("EaiClassAdapter",
                    $"FTP 上傳失敗\r\n目的路徑: {uploadUri}\r\n錯誤: {err}");

                throw;
            }
        }

        // =========================
        // Delete (Remote)
        // =========================
        public void Delete(string remotePath)
        {
            FtpWebRequest request = CreateRequest(
                remotePath,
                WebRequestMethods.Ftp.DeleteFile,
                null,
                null);

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                // 讀取 response 以確認刪除完成
                string status = response.StatusDescription;
                EaiComponent.WriteEventLog_Inf("EaiClassAdapter", $"FTP 刪除成功: {status}");
            }
        }

        // =========================
        // Helper Methods
        // =========================
        private void DownloadFile(string remoteFile, string localFile, ReceiveContext context)
        {
            FtpWebRequest request = CreateRequest(
                remoteFile,
                WebRequestMethods.Ftp.DownloadFile,
                context.User,
                context.Password);

            using (var response = (FtpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var fs = File.Create(localFile))
            {
                stream.CopyTo(fs);
            }
        }

        private FtpWebRequest CreateRequest(
            string uri,
            string method,
            string user,
            string password)
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = method;
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
            {
                request.Credentials = new NetworkCredential(user, password);
            }
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            return request;
        }
    }
}