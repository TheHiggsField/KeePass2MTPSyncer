using System.Collections.Generic;
using System.Threading.Tasks;

namespace LocalSync.TransferClients
{
    public interface ITransferClient
    {
        Task<bool> Download(string localRelativeUri, string serverRelativeUri);
        Task<bool> Upload(string localRelativeUri, string serverRelativeUri);

        Task<List<string>> List(string serverRelativeUri);

        Task<bool> IsConnected();

        string LocalStoreUri { get;}

    }
}