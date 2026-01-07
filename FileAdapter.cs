using System;
using System.Collections.Generic;
using System.IO;

namespace EaiClassAdapter
{
    public class FileAdapter : ITransferAdapter
    {
        // =========================
        // Receive (Download)
        // =========================
        public string[] Receive(ReceiveContext context)
        {
             var downloadedFiles = new List<string>();


            string path = context.ReceivePath;
            string mask = context.ReceiveFileMask;
            bool deleteAfterReceive = context.DeleteAfterReceive;            

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Source path '{path}' not found.");

            var files = Directory.GetFiles(path, mask);

            EaiComponent.WriteEventLog_Inf("EaiClassAdapter", "File Receive \r\n從來源路徑 " + path + @"\" +  mask  + " 收到 "+  files.Length.ToString() + " 個檔案");

            foreach (var file in files)
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "EaiJobTemp");
                Directory.CreateDirectory(tempDir);

                string destFile = Path.Combine(tempDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);

                downloadedFiles.Add(destFile);

                if (deleteAfterReceive)
                    File.Delete(file);
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

            string sendPath = context.SendPath;

            // ★★★ 關鍵修正：使用 SendFileNameFormat 來產生目的檔名 ★★★
            string fileName = FileNameFormatter.Format(
                context.SendFileNameFormat ?? "%SourceFileName%",  // 防空
                Path.GetFileName(localFilePath)                    // 傳入原始檔名
            );

            fileName = Utils.NormalizeString(fileName);

            string destFile = Path.Combine(sendPath, fileName);
                       
            //EaiComponent.WriteEventLog("error",$"[FileAdapter.Send] 來源: {localFilePath}");
            //EaiComponent.WriteEventLog("error", $"[FileAdapter.Send] 來源: {localFilePath}");
            //EaiComponent.WriteEventLog("error", $"[FileAdapter.Send] 格式: {context.SendFileNameFormat}");
            //EaiComponent.WriteEventLog("error", $"[FileAdapter.Send] 產生檔名: {fileName}");
            //EaiComponent.WriteEventLog("error", $"[FileAdapter.Send] 完整目的路徑: {destFile}");

            // 確保目的資料夾存在
            Directory.CreateDirectory(sendPath);

            string fileCopyMode = context.FileCopyMode?.ToUpper() ?? "OVERWRITE";

            if (File.Exists(destFile))
            {
                switch (fileCopyMode)
                {
                    case "OVERWRITE":
                        File.Copy(localFilePath, destFile, true);
                        break;

                    case "CREATENEW":
                        // 產生不重複檔名
                        string extension = Path.GetExtension(fileName);
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        destFile = Path.Combine(sendPath, $"{nameWithoutExt}_{Guid.NewGuid():N}{extension}");
                        File.Copy(localFilePath, destFile, false);
                        break;

                    case "APPEND":
                        // Append 只適合文字檔，二進位檔建議不要用
                        using (var fsDest = new FileStream(destFile, FileMode.Append, FileAccess.Write))
                        using (var fsSrc = File.OpenRead(localFilePath))
                        {
                            fsSrc.CopyTo(fsDest);
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

            // 建議加 log 確認（測試完可移除）
            // Console.WriteLine($"已複製檔案 → {destFile}");
        }

        // =========================
        // ListFiles (列出遠端檔案)
        // =========================
        public string[] ListFiles(string path, string fileMask, string user, string password)
        {
            if (!Directory.Exists(path))
                return Array.Empty<string>();

            return Directory.GetFiles(path, fileMask);
        }

        // =========================
        // Delete (Local)
        // =========================
        public void Delete(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
