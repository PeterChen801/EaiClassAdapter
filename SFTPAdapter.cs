using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace EaiClassAdapter
{
    public class SFTPAdapter : ITransferAdapter
    {

        public string[] ListFiles(string path, string mask, string user, string password)
        {
            var files = new List<string>();

            string listUri = path;
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
            var downloadedFiles = new List<string>();

            using (var client = CreateClient(context.Host, context.User, context.Password))
            {
                client.Connect();

                var files = client.ListDirectory(context.ReceivePath)
                                  .Where(f => !f.IsDirectory && !f.IsSymbolicLink);

                foreach (var file in files)
                {
                    if (!FileMaskMatcher.IsMatch(file.Name, context.ReceiveFileMask))
                        continue;

                    string localFile =
                        Path.Combine(context.LocalTempPath, file.Name);

                    using (var fs = File.Create(localFile))
                    {
                        client.DownloadFile(file.FullName, fs);
                    }

                    downloadedFiles.Add(localFile);

                    if (context.DeleteAfterReceive)
                    {
                        client.DeleteFile(file.FullName);
                    }
                }

                client.Disconnect();
            }

            return downloadedFiles.ToArray();
        }

        // =========================
        // Send (Upload)
        // =========================
        public void Send(string localFilePath, SendContext context)
        {
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException(localFilePath);

            using (var client = CreateClient(context.Host, context.User, context.Password))
            {
                client.Connect();

                string fileName = FileNameFormatter.Format(
                    context.SendFileNameFormat,
                    localFilePath);

                string remotePath =
                    CombineRemotePath(context.SendPath, fileName);

                using (var fs = File.OpenRead(localFilePath))
                {
                    client.UploadFile(fs, remotePath, true); // overwrite
                }

                client.Disconnect();
            }
        }

        // =========================
        // Delete (Remote)
        // =========================
        public void Delete(string remotePath)
        {
            using (var client = CreateClientByUri(remotePath))
            {
                client.Connect();
                client.DeleteFile(GetPathFromUri(remotePath));
                client.Disconnect();
            }
        }

        // =========================
        // Helper Methods
        // =========================
        private SftpClient CreateClient(string host, string user, string password)
        {
            return new SftpClient(host, user, password);
        }

        private SftpClient CreateClientByUri(string uri)
        {
            var u = new Uri(uri);

            return new SftpClient(
                u.Host,
                u.UserInfo.Split(':')[0],
                u.UserInfo.Split(':')[1]
            );
        }

        private string GetPathFromUri(string uri)
        {
            return new Uri(uri).AbsolutePath;
        }

        private string CombineRemotePath(string path, string file)
        {
            if (path.EndsWith("/"))
                return path + file;

            return path + "/" + file;
        }
    }
}
