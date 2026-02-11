using System.Collections.Generic;
using System.Threading.Tasks;

namespace LocalSync
{
    public interface ITransferClient
    {
        Task<bool> Download(string serverUri, string localUri);
        Task<bool> Upload(string localUri, string serverUri);

        List<string> List(string serverUri);

        Task<bool> IsConnected { get; }

    }
}