using System.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using KeePassLib.Security;

namespace LocalSync.TransferClients
{
    internal enum ClientType
    {
        Http = 0
    }

    internal class TransferClientFactory
    {
        public ClientType? ClientTypeToBuild { get; private set; } = null;
        public string EndPoint { get; private set; } = null;
        public ProtectedString SharedKey { get; private set; } = null;
        public string UserId { get; private set; } = null; 
        public string LocalTempUri { get; private set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KeePass",
            "TempDBs"
        );
        public List<string> Errors { get; private set; } = new List<string>();

        public TransferClientFactory WithLocalStorage(string localTempUri)
        {
            if (string.IsNullOrWhiteSpace(localTempUri))
            {
                Errors.Add("localTempUri '{localTempUri}' not set");
                return null;
            }

            LocalTempUri = localTempUri;
            return this;
        }

        public TransferClientFactory ConfigureHttpTransferClient(string endPoint, string userId, ProtectedString sharedKey)
        {

            if (string.IsNullOrWhiteSpace(endPoint))
                Errors.Add($"endPoint '{endPoint}' is not valid.");

            ClientTypeToBuild = ClientType.Http;
            EndPoint = endPoint;
            UserId = userId;
            SharedKey = sharedKey;
            return this;
        }

        public async Task<ITransferClient> Build()
        {
            try
            {
                Directory.CreateDirectory(LocalTempUri);

                switch (ClientTypeToBuild)
                {
                    case ClientType.Http:
                        return await HttpTransferClient.HttpTransferClient.Create(EndPoint, LocalTempUri, UserId, SharedKey);
                    default:
                        {
                            Errors.Append("The factory has not been configures yet - can't build yet.");
                            return null;
                        }
                }
            }
            catch (Exception ex)
            {
                Errors.Add(ex.ToString());
                return null;
            }
        }
    }
}
