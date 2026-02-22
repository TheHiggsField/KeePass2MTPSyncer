using System.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using KeePassLib.Security;
using KeePassLib;
using LocalSync.Extension;

namespace LocalSync.TransferClients
{
    public class HttpTransferClientConfig
    {
        public string SharedKeyPath {get; set; }
        public string SharedKeyUserId { get; set; }
        public string ServerEndpoint { get; set; }
        public string LocalTempUri { get; set; }
        public static HttpTransferClientConfig FromJson(string json)
        {
            return JsonConvert.DeserializeObject<HttpTransferClientConfig>(json);
        }
    }

    internal class HttpTransferClientFactory
    {
        public ClientType? ClientTypeToBuild { get; private set; } = null;
        public string EndPoint { get; private set; } = null;
        private ProtectedString SharedKey { get; set; } = null;
        public string UserId { get; private set; } = null; 
        public string LocalTempUri { get; private set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KeePass",
            "TempDBs"
        );

        public List<string> Errors { get; private set; } = new List<string>();

        public HttpTransferClientFactory WithLocalStorage(string localTempUri)
        {
            if (string.IsNullOrWhiteSpace(localTempUri))
            {
                Errors.Add("localTempUri '{localTempUri}' not set");
                return null;
            }

            LocalTempUri = localTempUri;
            return this;
        }

        public HttpTransferClientFactory SetSecrets(IList<PwDatabase> pwDatabases)
        {

            if (string.IsNullOrWhiteSpace(UserId))
                Errors.Add($"UserId '{UserId}' is not valid or has not been set yet.");

            foreach (var db in pwDatabases)
            {
                SharedKey = db.GetSharedKey(UserId);

                if (SharedKey != null)
                    break;

            }

            if (SharedKey == null)
                Errors.Add($"SharedKey for UserID '{UserId}' could not be found.");

            return this;
        }

        public HttpTransferClientFactory ConfigureHttpTransferClient(string endPoint, string userId)
        {

            if (string.IsNullOrWhiteSpace(endPoint))
                Errors.Add($"endPoint '{endPoint}' is not valid.");

            ClientTypeToBuild = ClientType.Http;
            EndPoint = endPoint;
            UserId = userId;
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

        public HttpTransferClientConfig GetConfig()
        {
            return new HttpTransferClientConfig()
            {
                ServerEndpoint = EndPoint,
                SharedKeyPath = UserId,
                SharedKeyUserId = UserId,
                LocalTempUri = LocalTempUri
            };
        }

        public static HttpTransferClientFactory FromConfig(HttpTransferClientConfig config, IList<PwDatabase> pwDatabases)
        {
            return new HttpTransferClientFactory()
                .WithLocalStorage(config.LocalTempUri)
                .ConfigureHttpTransferClient(config.ServerEndpoint, config.SharedKeyPath)
                .SetSecrets(pwDatabases);

        }
    }
}
