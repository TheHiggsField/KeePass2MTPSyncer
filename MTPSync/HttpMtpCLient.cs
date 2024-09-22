using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Web;
using System.Threading.Tasks;
using Zeroconf;
using System.Windows.Forms;

namespace MTPSync
{
    public class HttpMtpClient : IMTPClient
    {
        private readonly HttpClient httpClient = new HttpClient();
        private String serverIP = null;
        private int? serverPort = null;
        private bool serverDiscovered => !String.IsNullOrEmpty(serverIP) && serverPort.HasValue;
        private String EndPoint => serverDiscovered ? $"http://{serverIP}:{serverPort}" : null;

        private readonly String serviceDisplayName = "two-girls-and-a-cat";
        private readonly String serviceKey = "two-girls-and-a-cat._http._tcp.local.";
        private readonly String serviceType = "_http._tcp.local.";


        public async Task<bool> TryDiscoverServer(string mtpPath)
        {
            var results = await ZeroconfResolver.ResolveAsync(serviceType);

            Console.WriteLine($"Number og elementens in mDNS result {results.Count}");


            var host = results.FirstOrDefault(x => x.DisplayName == serviceDisplayName);

            serverIP = host?.IPAddress;
            IService service = null;

            host?.Services.TryGetValue(serviceKey, out service);

            serverPort = service?.Port;


            Console.WriteLine($"Service: {service?.ServiceName} rawEndpoint: \"{serverIP}:{serverPort}\"");

            return serverDiscovered;
        }

        public bool Download(string mtpPath, string localPath)
        {
            var fileName = mtpPath;

            Console.WriteLine($"{EndPoint}/upload/{fileName}");
            var response = httpClient.GetAsync($"{EndPoint}/download/{fileName}").GetAwaiter().GetResult();

            using (var fs = new FileStream(localPath, FileMode.Truncate))
            {
                response.Content.CopyToAsync(fs).Wait();
            }


            return true;
        }

        public bool Upload(string localPath, string mtpPath)
        {
            var fileName = Path.GetFileName(localPath);
            string mimeType = null;

            switch (Path.GetExtension(localPath))
            {
                case ".kbdx":
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

                Console.WriteLine($"{EndPoint}/upload/{fileName}");
                response = httpClient.PostAsync($"{EndPoint}/upload/{fileName}", fileContent).GetAwaiter().GetResult();

            }

            return response.IsSuccessStatusCode;
        }

        public List<string> List(string mtpPath)
        {
            var response = httpClient.GetAsync(EndPoint + "/list").GetAwaiter().GetResult();

            var listStr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            return listStr.Split(';').ToList();
        }



        public bool IsConnected
        {
            get
            {
                if (!serverDiscovered)
                {
                    TryDiscoverServer(null).Wait();
                }

                if (serverDiscovered)
                {
                    return httpClient.GetAsync(EndPoint).Result.IsSuccessStatusCode;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsFolder(string mtpPath)
        {
            return true;
        }


    }
}
