using System;

namespace EaiClassAdapter
{
    public interface ITransferAdapter
    {
        // =========================
        // File/FTP Receive
        // =========================
        string[] Receive(ReceiveContext context);

        // =========================
        // File/FTP Send
        // =========================
        void Send(string localFilePath, SendContext context);

        // =========================
        // Delete Remote/File
        // =========================
        void Delete(string remotePath);

        // =========================
        // List Files
        // =========================
        string[] ListFiles(string path, string fileMask, string user, string password);
    }
}
