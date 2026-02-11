using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LocalSync.TransferClients.HttpClient
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

        private List<string> trustedCertList = new List<string>();

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
            if (!string.IsNullOrEmpty(path))
            {
                var config = new ServerConfig(path.Replace('\\', '/'));

                if (config.serverDiscovered)
                {
                    serverConfig = config;
                    bool result = VerifyServerCert().Result;
                }

            }
        }

        public bool Download(string serverUri, string localUri)
        {
            var fileName = Path.GetFileName(serverUri);

            Console.WriteLine($"{serverConfig?.EndPoint}/download/{fileName}");

            var response = httpClient.GetAsync($"{serverConfig?.EndPoint}/download/{fileName}").GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                using (var fs = new FileStream(localUri, FileMode.Create))
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

        public bool Upload(string localUri, string serverUri)
        {
            var fileName = Path.GetFileName(localUri);
            string mimeType = null;

            switch (Path.GetExtension(localUri))
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

            using (var fileStream = new FileStream(localUri, FileMode.Open))
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

        public List<string> List(string serverUri)
        {
            var response = httpClient.GetAsync(serverConfig?.EndPoint + "/list").GetAwaiter().GetResult();

            var listStr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            return listStr.Split(';').ToList();
        }


        /*
         {
            UUID:"c5913314-76ef-41ed-aa1f-0b1e3026e916",
            certificate-sha-256-hash:"23a0642e1f7b259e037e0735cedd11f45228bb36325510d7230003cdf88ac3c0",
            signature:"c90e843fa14a74e785c0f64f145fdad0e4f465eff4c819e5b04783383221fc4b"
            signature-version:v0
        }
         */
        public class SignedCertResponse
        {
            public string UUID { get; set; }
            [JsonProperty("certificate-sha-256-hash")]
            public string certificate_sha_256_hash { get; set; }
            public string signature { get; set; }
            [JsonProperty("signature-version")]
            public string signature_version { get; set; }

            public static SignedCertResponse FromJson(string json)
            {
                return JsonConvert.DeserializeObject<SignedCertResponse>(json);
            }
        }

        public async Task<bool> VerifyServerCert()
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(serverConfig?.EndPoint + "/cert"),
                Method = HttpMethod.Get
            };

            request.Headers.Add("FileShare-UserId", "Desktop");

            var response = await httpClient.SendAsync(request).Result.Content.ReadAsStringAsync();

            SignedCertResponse signedMessage = SignedCertResponse.FromJson(response);
            var sharedKey = "0123456789ABCDEF";

            var expectedSignature = SignMessage(signedMessage.UUID + signedMessage.certificate_sha_256_hash, sharedKey);

            if (expectedSignature == signedMessage.signature)
            {
                trustedCertList.Append(signedMessage.certificate_sha_256_hash);
                return true;
            }
            else
            {
                return false;
            }
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

        public static string SignMessage(string message, string sharedKey)
        {

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] keyBytes = Encoding.UTF8.GetBytes(sharedKey);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(messageBytes.Concat(keyBytes).ToArray());
            hash = hashstring.ComputeHash(keyBytes.Concat(hash).ToArray());
            string hashString = string.Empty;

            foreach (byte x in hash)
            {
                hashString += string.Format("{0:x2}", x);
            }

            return hashString;
        }

        /// <summary>
        /// This doesn't really make sense for this client, so just check if it works as the base adresse.
        /// </summary>
        /// <param name="serverUri"></param>
        /// <returns></returns>
        public bool IsFolder(string serverUri)
        {
            var currentConfig  = serverConfig;
            bool result = VerifyServerCert().Result;

            serverConfig = new ServerConfig(serverUri);

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
