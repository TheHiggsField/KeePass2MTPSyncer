using System.Collections.Generic;

namespace MTPSync
{
    public interface MTPAccess
    {
        bool Download(string mtpPath, string destinationPath);
        bool Upload(string sourcePath, string mtpPath);

        List<string> List(string mtpSourcePath);
    }
}