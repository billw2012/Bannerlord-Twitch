using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BLTConfigure
{
    public static class TwitchAuthHelper
    {
        private const string HttpRedirect = 
            @"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <title>Twitch Token Auth Redirection</title>
            </head>
            <body>
                <h1 style=""color: #1cb425"">You can close this and go back to the Bannerlord Twitch window now!</h1>
                <noscript>
                    <h1>You must have javascript enabled for OAuth redirection to work!</h1>
                </noscript>
                <script lang=""javascript"">
                    let req = new XMLHttpRequest();
                    req.open('POST', '/', false);
                    req.setRequestHeader('Content-Type', 'text');
                    req.send(document.location.hash);
                    window.close();
                </script>
            </body>
            </html>
            ";

        private static HttpListener listener;

        private const int Port = 18211;
        public const string ClientID = "spo54cze6gxb3zs5qrq4njistimg87";
        
        private static readonly HttpClient client = new HttpClient();
        
        public static async Task<string> Authorize(params string[] scopes)
        {
            // Make sure we close before we try again
            listener?.Close();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{Port}/");
            listener.Start();

            string scopeStr = string.Join(" ", scopes);
            Process.Start(
                $"https://id.twitch.tv/oauth2/authorize?client_id={ClientID}" +
                $"&redirect_uri=http%3A%2F%2Flocalhost%3A{Port}" +
                $"&response_type=token" +
                $"&scope={scopeStr}");

            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                var request = context.Request;
                if (request.HttpMethod == "POST")
                {
                    string text;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        text = await reader.ReadToEndAsync();
                    }
                    listener.Close();
                    
                    var keyValuePairs = HttpUtility.UrlDecode(text)
                        .Split('&')
                        .Select(kv => kv.Split('='))
                        .ToDictionary(kv => kv[0].Replace("#", ""), kv => kv[1]);
                    if (keyValuePairs.TryGetValue("access_token", out var accessToken))
                    {
                        return accessToken;
                    }
                }
                else
                {
                    var response = context.Response;
                    byte[] buffer = Encoding.UTF8.GetBytes(HttpRedirect);
                    response.ContentLength64 = buffer.Length;
                    var output = response.OutputStream;
                    await output.WriteAsync(buffer, 0, buffer.Length);
                    output.Close();
                }
            }

            return null;
        }

        public static void CancelAuth() => listener?.Close();

        public static async Task<bool> TestAPIToken(string token)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                client.DefaultRequestHeaders.Add("Client-Id", ClientID);
                var response = await client.GetAsync("https://api.twitch.tv/helix/users");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}