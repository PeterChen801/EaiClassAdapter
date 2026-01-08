using System;
using System.Collections.Generic;
using System.IO;

namespace EaiClassAdapter
{
    public class FileAdapter : ITransferAdapter
    {
        // =========================
        // Receive (Local → Temp)
        // =========================
        public string[] Receive(ReceiveContext context)
        {
            var receivedFiles = new List<string>();

            string sourcePath = context.ReceivePath;
            string mask = context.ReceiveFileMask;
            bool deleteAfterReceive = context.DeleteAfterReceive;

            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source path '{sourcePath}' not found.");

            var files = Directory.GetFiles(sourcePath, mask);

            // Job 專用 temp folder（避免多執行緒衝突，不污染檔名）
            string jobTempDir = Path.Combine(
                Path.GetTempPath(),
                "EaiJobTemp",
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(jobTempDir);

            context.JobTempFolder = jobTempDir; // 儲存給 TransferBinding 統一刪除

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file); // 保留原始檔名
                string tempFile = Path.Combine(jobTempDir, fileName);

                File.Copy(file, tempFile, true);
                receivedFiles.Add(tempFile);

                if (deleteAfterReceive)
                {
                    File.Delete(file);
                }
            }

            return receivedFiles.ToArray();
        }

        // =========================
        // Send (Local → Local)
        // =========================
        public void Send(string localFilePath, SendContext context)
        {
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException(localFilePath);

            string sendPath = context.SendPath;
            Directory.CreateDirectory(sendPath);

            string sourceFileName = Path.GetFileName(localFilePath);

            string finalFileName = FileNameFormatter.Format(
                context.SendFileNameFormat ?? "%SourceFileName%",
                sourceFileName
            );
            finalFileName = Utils.NormalizeString(finalFileName);

            string destFile = Path.Combine(sendPath, finalFileName);
            string copyMode = context.FileCopyMode?.ToUpper() ?? "OVERWRITE";

            if (File.Exists(destFile))
            {
                switch (copyMode)
                {
                    case "OVERWRITE":
                        File.Copy(localFilePath, destFile, true);
                        break;

                    case "CREATENEW":
                        string ext = Path.GetExtension(finalFileName);
                        string nameOnly = Path.GetFileNameWithoutExtension(finalFileName);
                        destFile = Path.Combine(sendPath, $"{nameOnly}_{Guid.NewGuid():N}{ext}");
                        File.Copy(localFilePath, destFile, false);
                        break;

                    case "APPEND":
                        using (var dest = new FileStream(destFile, FileMode.Append, FileAccess.Write, FileShare.None))
                        using (var src = File.Open(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            src.CopyTo(dest);
                        }
                        break;

                    default:
                        File.Copy(localFilePath, destFile, true);
                        break;
                }
            }
            else
            {
                File.Copy(localFilePath, destFile, true);
            }

            EaiComponent.WriteEventLog_Inf(
                "EaiClassAdapter",
                $"File Send 成功\r\n來源: {localFilePath}\r\n目的: {destFile}"
            );
        }

        // =========================
        // ListFiles
        // =========================
        public string[] ListFiles(string path, string fileMask, string user, string password)
        {
            if (!Directory.Exists(path))
                return Array.Empty<string>();

            return Directory.GetFiles(path, fileMask);
        }

        // =========================
        // Delete
        // =========================
        public void Delete(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
