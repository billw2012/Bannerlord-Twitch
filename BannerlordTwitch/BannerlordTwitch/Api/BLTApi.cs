using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using Newtonsoft.Json;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Internal;

namespace BannerlordApi
{
    public class BLTApi
    {
        List<BLTApiUrl> bltApiUrls = new List<BLTApiUrl>();
        public readonly int port = 9000;
        private static HttpListener listener;
        private readonly Settings settings = Settings.Load();

        public BLTApi()
        {
            _ = RunServerAsync();
            bltApiUrls.Add(new BLTApiUrl("GET", "", "Show all registered api path", OnApiVisit));
            bltApiUrls.Add(new BLTApiUrl("GET", "command", "Used to execute commands", OnCommand));
        }

        public async Task RunServerAsync()
        {
            Log.Trace("Api server starting");
            bool runServer = true;
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:"+port+"/api/");
            listener.Start();

            while (runServer)
            {
                try
                {
                    HttpListenerContext ctx = await listener.GetContextAsync();

                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    OnUrlRequest(req, resp);
                }
                catch (Exception e)
                {
                    Log.Exception("Exception handling request",e);
                }
            }
        }

        public void OnUrlRequest(HttpListenerRequest req, HttpListenerResponse resp)
        {
            Log.Trace("[VISITED]" + req.Url.ToString());

            bltApiUrls.ForEach(bltApiUrl =>
            {
                Uri existingUrl =  new Uri("http://localhost:" + port + "/api/" + bltApiUrl.url);
                
                if (Uri.Compare(existingUrl, req.Url, UriComponents.Path, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    Log.Trace("[EXEC VISITED]" + bltApiUrl.url);
                    try { 
                        bltApiUrl.onVisit.Invoke(req,resp);
                    }catch(Exception e){
                        Log.Exception("Issue on visit api urls from BLTApiUrl : " + bltApiUrl.url,e);
                    }
                }
            });
        }

        public void OnApiVisit(HttpListenerRequest req, HttpListenerResponse resp)
        {
            dynamic json = new ExpandoObject();
            string url = HttpUtility.UrlDecode(req.Url.ToString());
            json.url = url;
            SendJSON(resp, json);
        }
        public void OnCommand(HttpListenerRequest req, HttpListenerResponse resp)
        {
            dynamic json = new ExpandoObject();

            string cmdName;
            string userId;
            string args;

            var splittedUrl = req.Url.ToString().Split('?');
            if (splittedUrl.Length > 1)
            {
                var keyValuePairs = HttpUtility.UrlDecode(splittedUrl[1])
                .Split('&')
                .Select(kv => kv.Split('='))
                .ToDictionary(kv => kv[0], kv => kv.Count() > 1 ? kv[1] : null);
                            keyValuePairs.TryGetValue("cmd", out cmdName);
                            keyValuePairs.TryGetValue("userId", out userId);
                            keyValuePairs.TryGetValue("args", out args);
            }
            else
            {
                cmdName = null;
                userId = null;
                args = null;
            }


            if (cmdName == null || userId == null)
            {
                json.error = "Missing variables";
                json.expected = "cmd=string&pseudo=string&args=string";
                json.actualParams = "cmd=" + cmdName + "&userId=" + userId + "&args=" + args;
            }
            else
            {
                MainThreadSync.Run(() => {
                    var client = BLTModule.TwitchService?.GetClientFromClientId(userId);
                    var commandFound = false;

                    json.actualParams = "cmd=" + cmdName + "&userId=" + userId + "&args=" + args;

                    //if client isn't found
                    if(client == null)
                    {
                        json.error = "Can't find a twitch user with this id OR twitch api isn't started yet.";
                        SendJSON(resp, json);
                    }
                    else
                    {
                        json.identifiedUser = client;
                        commandFound = (bool)(BLTModule.TwitchService?.TestCommand(cmdName, client.DisplayName, args));
                        //if command isn't found
                        if (commandFound == false)
                        {
                            json.error = "Can't find a command with this name";
                        }

                        SendJSON(resp, json);
                    }
                });
            }

        }

        private void SendJSON(HttpListenerResponse resp, Object obj)
        {
            string jsonString = JsonConvert.SerializeObject(obj);
            byte[] data = Encoding.UTF8.GetBytes(jsonString);
            resp.ContentType = "text/json";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;

            resp.OutputStream.Write(data, 0, data.Length);
            resp.Close();
        }
    }
}