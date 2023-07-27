using System.Collections.Generic;

namespace MTPSync
{
    public interface MTPAccess
    {
        bool Copy(string SourcePath, string destinationPath);

        List<string> List(string mtpSourcePath);
    }
}