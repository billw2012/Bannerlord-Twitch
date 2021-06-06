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
        public dynamic commandCallbackMessage;
        private readonly Settings settings = Settings.Load();

        public BLTApi()
        {
            _ = RunServerAsync();
            bltApiUrls.Add(new BLTApiUrl("GET", "", "Show all registered api path", OnApiVisit));
            bltApiUrls.Add(new BLTApiUrl("GET", "command", "Used to execute commands", OnCommand));
        }

        public async Task RunServerAsync()
        {
            Trace.WriteLine("Api server starting");
            bool runServer = true;
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:"+port+"/api/");
            listener.Start();

            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                OnUrlRequest(req, resp);
            }
        }

        public void OnUrlRequest(HttpListenerRequest req, HttpListenerResponse resp)
        {
            Trace.WriteLine("[VISITED]" + req.Url.ToString());
            bltApiUrls.ForEach(bltApiUrl =>
            {
                if (
                    "http://localhost:"+port+"/api/" + bltApiUrl.url == req.Url.ToString().Split('?')[0] || 
                    "http://localhost:" + port + "/api/" + bltApiUrl.url + "/" == req.Url.ToString().Split('?')[0] || 
                    "http://localhost:"+port+ "/api" + bltApiUrl.url == req.Url.ToString().Split('?')[0]
                )
                {
                    Trace.WriteLine("[EXEC VISITED]" + bltApiUrl.url);
                    try { 
                        bltApiUrl.onVisit.Invoke(req,resp);
                    }catch(Exception e){
                        Log.LogFeedSystem("Issue on visit api urls from BLTApiUrl : " + bltApiUrl.url);
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

            string cmdName = "";
            string pseudo = "";
            string args = "";

            try
            {
                var splittedUrl = req.Url.ToString().Split('?');
                //if url have option
                var splittedOption = splittedUrl[1].Split('&');
                //foreach option
                for (int i = 0; i < splittedOption.Length; i++)
                {
                    string[] option = splittedOption[i].Split('=');
                    //check key & value
                    if (option[0] != null && option[1] != null)
                    {
                        switch (option[0])
                        {
                            case "cmd":
                                cmdName = option[1];
                                break;
                            case "pseudo":
                                pseudo = option[1];
                                break;
                            case "args":
                                args = option[1];
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                json.error = "Missing variables";
                json.expected = "cmd=string&pseudo:string&args:string";
                json.actualParams = "cmd=" + cmdName + "&pseudo=" + pseudo + "&args=" + args;
            }

            if (cmdName == "" || pseudo == "" || args == "")
            {
                json.error = "Missing variables";
                json.expected = "cmd=string&pseudo:string&args:string";
                json.actualParams = "cmd=" + cmdName + "&pseudo=" + pseudo + "&args=" + args;
            }
            else
            {
                json.actualParams = "cmd=" + cmdName + "&pseudo=" + pseudo + "&args=" + args;
                json.status = BLTModule.TwitchService?.TestCommand(cmdName, pseudo, args);

                if(json.status == true || json.status == false)
                {
                    json.commandCallback = commandCallbackMessage;
                    commandCallbackMessage = null;
                }
            }

            SendJSON(resp, json);
        }

        private bool cmdExec(string cmdName, string userName, string args)
        {
            var cmd = settings.EnabledCommands.FirstOrDefault(c => c.Name == cmdName);
            if (cmd == null)
                return false;

            var context = ReplyContext.FromUser(cmd, userName, args);
            ActionManager.HandleCommand(cmd.Handler, context, cmd.HandlerConfig);
            return true;
        }

        public void SendJSON(HttpListenerResponse resp, Object obj)
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