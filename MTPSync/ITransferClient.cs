using System.Collections.Generic;

namespace MTPSync
{
    public interface ITransferClient
    {
        bool Download(string mtpPath, string localPath);
        bool Upload(string localPath, string mtpPath);

        List<string> List(string mtpPath);

        bool IsConnected { get; }

        bool IsFolder(string mtpPath);

    }
}