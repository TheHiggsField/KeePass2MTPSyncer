using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using KeePassLib.Security;
using Newtonsoft.Json;

namespace LocalSync.TransferClients.HttpTransferClient
{

    public class HttpTransferClient : ITransferClient
    {

        private readonly List<string> trustedCertList = new List<string>();

        private readonly HttpClient httpClient;

        private readonly ProtectedString sharedKey;

        private readonly string userId;

        private readonly Uri serverBaseUri;


        public string LocalStoreUri { get; private set; } = null;

        private HttpTransferClient(string _serverEndPoint, string _userId, ProtectedString _sharedKey)
        {
            serverBaseUri = new Uri(_serverEndPoint);

            httpClient = SetupHttpClient(
                serverBaseUri, (certHash) => trustedCertList.Contains(certHash)
            );

            sharedKey = _sharedKey;
            userId = _userId;
        }

        private static bool OnlyChainIssues(SslPolicyErrors sslPolicyErrors)
        {
            SslPolicyErrors flags = 0;

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
                flags |= SslPolicyErrors.RemoteCertificateChainErrors;
            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
                flags |= SslPolicyErrors.RemoteCertificateNameMismatch;

            return sslPolicyErrors == flags;
        }

        private static HttpClient SetupHttpClient(
            Uri serverBaseUri, Func<X509Certificate2, bool> serverCertificateCustomValidationCallback
        )
        {
            return new HttpClient(
                new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, certChain, sslPolicyErrors) =>
                        OnlyChainIssues(sslPolicyErrors) && serverCertificateCustomValidationCallback(cert)
                }
            )
            {
                BaseAddress = serverBaseUri
            };
        }

        private static HttpClient SetupHttpClient(
            Uri serverBaseUri, Func<string, bool> serverCertificateValidationCallback
        ) => SetupHttpClient(
            serverBaseUri,
            (cert) => serverCertificateValidationCallback(
                GetHashInHex(cert.GetRawCertData())
            )
        );


        public static async Task<HttpTransferClient> Create(string _serverEndPoint, string _localStoreUri, string userId, ProtectedString sharedKey)
        {

            if (
                string.IsNullOrEmpty(_serverEndPoint) || !Uri.IsWellFormedUriString(_serverEndPoint, UriKind.Absolute)
            )
                throw new ArgumentException($"unacceptable serverEndPoint {_serverEndPoint}");

            var client = new HttpTransferClient(_serverEndPoint, userId, sharedKey);



            if (!(new string[] { "http", "https" }.Contains(client.serverBaseUri.Scheme))) 
            throw new ArgumentException(
                $"ServerEndPoint should have scheme http or https, not '{client.serverBaseUri.Scheme}'."
            );


            if (client.serverBaseUri.Scheme == "https")
            {
                var serverCertValidationError = await client.ServerCertValidationError();
                if (serverCertValidationError != null)
                    throw new CryptographicException($"Could not Verify server Cert:\n{serverCertValidationError}");

                if (!await client.IsConnected())
                    throw new Exception("Successfully verified certificate from server, but could not very a normal connection afterwards.");
            }
            else
            {
                if (await client.List(string.Empty) == null)
                    throw new Exception("The server didn't return a proper response.");
            }

            if (string.IsNullOrEmpty(_localStoreUri))
                throw new ArgumentException("unacceptable localStoreUri.");

            client.LocalStoreUri = _localStoreUri;


            return client;
        }

        public async Task<bool> Download(string serverUri, string localUri)
        {
            Console.WriteLine($"{serverBaseUri}/download/{serverUri}");

            var response = await httpClient.GetAsync($"/download/{serverUri}");

            if (!response.IsSuccessStatusCode)
                return false;

            using (var fs = new FileStream(Path.Combine(LocalStoreUri, localUri), FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }

            return true;
        }

        public async Task<bool> Upload(string serverUri, string localUri)
        {
            string mimeType;

            switch (Path.GetExtension(localUri))
            {
                case ".txt":
                    mimeType = "text/plain";
                    break;
                case ".kdbx":
                case ".dll":
                default:
                    mimeType = "application/octet-stream";
                    break;

            }

            HttpResponseMessage response;

            using (var fileStream = new FileStream(Path.Combine(LocalStoreUri, localUri), FileMode.Open))
            {
                byte[] buffer = new byte[fileStream.Length];

                await fileStream.ReadAsync(buffer, 0, buffer.Length);

                fileStream.Seek(0, SeekOrigin.Begin);
                var fileContent = new StreamContent(fileStream);

                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
                fileContent.Headers.Add("FileShare-Signature", SignMessage(buffer, sharedKey));
                fileContent.Headers.Add("FileShare-UserId", userId);


                Console.WriteLine($"{serverBaseUri}/upload/{serverUri}");
                response = await httpClient.PostAsync($"/upload/{serverUri}", fileContent);

            }

            return response.IsSuccessStatusCode;
        }

        public async Task<List<string>> List(string _)
        {
            var response = await httpClient.GetAsync("/list");

            var listStr = await response.Content.ReadAsStringAsync();

            return listStr.TrimEnd(';').Split(';').ToList();
        }

        public async Task<bool> IsConnected()
        {
            try
            {
                return await List(string.Empty) != null;
            }
            catch
            {
                return false;
            }
        }

        /*
         {
            "UUID":"c5913314-76ef-41ed-aa1f-0b1e3026e916",
            "certificate-sha-256-hash":"23a0642e1f7b259e037e0735cedd11f45228bb36325510d7230003cdf88ac3c0",
            "signature":"c90e843fa14a74e785c0f64f145fdad0e4f465eff4c819e5b04783383221fc4b"
            "signature-version":v0
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

        public async Task<string> ServerCertValidationError()
        {
            if (serverBaseUri == null)
                throw new Exception("Invalid Endpoint");

            SignedCertResponse signedMessage;
            string certHashString = null;

            using (
                var unsafeHttpClient = SetupHttpClient(
                    serverBaseUri, (certHash) => {
                        certHashString = certHash;

                        return true; // <-- Unsafe to allow unverified cert only for initial  validation
                    }
                )
            ){
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(serverBaseUri + "/cert"),
                    Method = HttpMethod.Get
                };

                request.Headers.Add("FileShare-UserId", userId);
                request.Headers.ConnectionClose = true;

                var response = await unsafeHttpClient.SendAsync(request);

                if (!(response.Content.Headers.ContentType.MediaType == "application/json"))
                    return $"Server response content type should be 'application/json', not {response.Content.Headers.ContentType.MediaType}\n" +
                        $"Response content: {await response.Content.ReadAsStringAsync()}.";

                var responseJson = await response.Content.ReadAsStringAsync();

                signedMessage = SignedCertResponse.FromJson(responseJson);
            }

            var knownSignatureVersions = new string[] { "v0" };

            if (!knownSignatureVersions.Contains(signedMessage.signature_version))
                return $"Unknown signature version '{signedMessage.signature_version}', should be one of {knownSignatureVersions}";

            var expectedSignature = SignMessage(signedMessage.UUID + signedMessage.certificate_sha_256_hash, sharedKey);

            if (expectedSignature != signedMessage.signature)
                return $"Actual signature of received message did not match the claimed signature.";

            if (certHashString != signedMessage.certificate_sha_256_hash.ToUpper())
                return $"Actual message cert hash '{certHashString}', " +
                    $"doesn't match claimed cert hash '{signedMessage.certificate_sha_256_hash}'.";

            trustedCertList.Add(certHashString);

            return null;
        }

        private static string GetHashInHex(byte[] data)
        {
            using (SHA256Managed hasher = new SHA256Managed())
            {
                var hash = hasher.ComputeHash(data);

                return AsHexString(hash);
            }
        }

        public static string SignMessage(string message, ProtectedString sharedKey)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            return SignMessage(messageBytes, sharedKey);
        }

        public static string SignMessage(byte[] messageBytes, ProtectedString sharedKey)
        {

            if (sharedKey == null)
                return "No key available to sign with.";

            byte[] keyBytes = sharedKey.ReadUtf8();
            byte[] hash;

            using (SHA256Managed hasher = new SHA256Managed())
            {
                hash = hasher.ComputeHash(messageBytes.Concat(keyBytes).ToArray());
                hash = hasher.ComputeHash(keyBytes.Concat(hash).ToArray());
            }

            return AsHexString(hash);
        }

        private static string AsHexString(byte[] hash)
        {
            StringBuilder sb = new StringBuilder();

            foreach (byte x in hash)
            {
                sb.Append(string.Format("{0:X2}", x));
            }

            return sb.ToString();
        }

    }
}
