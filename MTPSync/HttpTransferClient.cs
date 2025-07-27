using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading.Tasks;
using Zeroconf;

namespace MTPSync
{
    public struct ServiceConfig
    {
        public string Name;
        public string Scheme;
        public string Type => $"_{Scheme}._tcp.local.";
        public string Key => Name + '.' + Type;
    }

    public struct ServerConfig
    {
        public String serverIP;
        public int? serverPort;
        public String serverScheme;

        public ServerConfig(string value)
        {
            if (Uri.IsWellFormedUriString(value, UriKind.Absolute))
            {
                //UriFormatException
                var uri = new Uri(value);

                serverIP = uri.Host;
                serverPort = uri.Port;
                serverScheme = uri.Scheme;
            }
            else
            {
                serverIP = string.Empty;
                serverPort = null;
                serverScheme = string.Empty;
            }
        }

        public bool serverDiscovered => (
            !String.IsNullOrEmpty(serverScheme) &&
            !String.IsNullOrEmpty(serverIP) &&
            serverPort.HasValue
        );

        public String EndPoint
        {
            get => serverDiscovered ? $"{serverScheme}://{serverIP}:{serverPort}" : null;
        }
    }

    public class HttpTransferClient : ITransferClient
    {
        private readonly HttpClient httpClient = new HttpClient(
            new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                }
            }
        )
        {
            Timeout = TimeSpan.FromMinutes(5),
        };

        private ServerConfig? serverConfig = null;

        private ServiceConfig httpServiceConfig = new ServiceConfig()
        {
            Name = "two-girls-and-a-cat",
            Scheme = "http"
        };

        private ServiceConfig httpsServiceConfig = new ServiceConfig()
        {
            Name = "two-girls-and-a-cat",
            Scheme = "https"
        };

        public HttpTransferClient(string path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                var config = new ServerConfig(path.Replace('\\', '/'));

                if (config.serverDiscovered)
                {
                    serverConfig = config;
                }

            }
        }

        public async Task<bool> TryDiscoverServer(string mtpPath)
        {
            serverConfig = await tryDiscoverServiceType(httpsServiceConfig) ?? await tryDiscoverServiceType(httpServiceConfig);

            Console.WriteLine(serverConfig?.EndPoint ?? null);
            return serverConfig != null;
        }

        private async Task<ServerConfig?> tryDiscoverServiceType(ServiceConfig serviceConfig)
        {
            var results = await ZeroconfResolver.ResolveAsync(serviceConfig.Type);

            if (results == null || results.Count == 0)
                return null;

            var host = results.FirstOrDefault(x => x.DisplayName == serviceConfig.Name);

            IService service = null;

            host?.Services.TryGetValue(serviceConfig.Key, out service);
            
            var res = new ServerConfig()
            {
                serverIP = host?.IPAddress,
                serverPort = service?.Port,
                serverScheme = serviceConfig.Scheme
            };

            return res.serverDiscovered ? (ServerConfig?)res : null;
        }

        public bool Download(string mtpPath, string localPath)
        {
            var fileName = Path.GetFileName(mtpPath);

            Console.WriteLine($"{serverConfig?.EndPoint}/download/{fileName}");

            var response = httpClient.GetAsync($"{serverConfig?.EndPoint}/download/{fileName}").GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                using (var fs = new FileStream(localPath, FileMode.Create))
                {

                    response.Content.CopyToAsync(fs).Wait();
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Upload(string localPath, string mtpPath)
        {
            var fileName = Path.GetFileName(localPath);
            string mimeType = null;

            switch (Path.GetExtension(localPath))
            {
                case ".kdbx":
                    mimeType = "application/octet-stream";
                    break;
                case ".txt":
                    mimeType = "text/plain";
                    break;
                default:
                    mimeType = "text/plain";
                    break;

            }

            HttpResponseMessage response;

            using (var fileStream = new FileStream(localPath, FileMode.Open))
            {
                //var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(fileStream);



                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);

                //content.Add(fileContent, "file", fileName);

                Console.WriteLine($"{serverConfig?.EndPoint}/upload/{fileName}");
                response = httpClient.PostAsync($"{serverConfig?.EndPoint}/upload/{fileName}", fileContent).GetAwaiter().GetResult();

            }

            return response.IsSuccessStatusCode;
        }

        public List<string> List(string mtpPath)
        {
            var response = httpClient.GetAsync(serverConfig?.EndPoint + "/list").GetAwaiter().GetResult();

            var listStr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            return listStr.Split(';').ToList();
        }

        public bool IsConnected
        {
            get
            {
                /*
                if (!(serverConfig?.serverDiscovered ?? false))
                {
                    TryDiscoverServer(null).Wait();
                }
                */

                //httpClient.DefaultRequestHeaders.Add("Connection", "close");

                if ((serverConfig?.serverDiscovered ?? false))
                {
                    try
                    {
                        return List("Irrelevant") != null;
                    }
                    catch ( Exception e)
                    {
                        Debug.Assert(false, "Error getting response from server:\n" + e.Message );
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// This doesn't really make sense for this client, so just check if it works as the base adresse.
        /// </summary>
        /// <param name="mtpPath"></param>
        /// <returns></returns>
        public bool IsFolder(string mtpPath)
        {
            var currentConfig  = serverConfig;

            serverConfig = new ServerConfig(mtpPath);

            if (IsConnected)
            {
                return true;
            }
            else
            {
                serverConfig = currentConfig;
                return false;
            }
        }


    }
}
