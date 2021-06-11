using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using BannerlordTwitch;
using BannerlordTwitch.Api;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using Newtonsoft.Json;
using SuperSocket.SocketBase;
using SuperWebSocket;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Internal;

namespace BannerlordApi
{
    public class BLTApi
    {
        List<BLTApiUrl> bltApiUrls = new List<BLTApiUrl>();
        private static HttpListener listener;
        private readonly Settings settings = Settings.Load();
        private static WebSocketServer wsServer;

        public BLTApi()
        {
            _ = RunServerAsync();
            bltApiUrls.Add(new BLTApiUrl("GET", "", "Show all registered api path", OnApiVisit));
            bltApiUrls.Add(new BLTApiUrl("GET", "commands", "Used to get all commands available", OnCommands));
            bltApiUrls.Add(new BLTApiUrl("GET", "ngrok", "Used to set the ngrok url", OnNgrok));
            bltApiUrls.Add(new BLTApiUrl("GET", "ngrok", "Used to set the ngrok url", OnNgrok));
        }

        public async Task RunServerAsync()
        {
            Log.Trace("Api server starting");

            //SOCKET (Used when we have callback to do)
            wsServer = new WebSocketServer();
            int port = 9100;
            wsServer.Setup(port);
            wsServer.NewSessionConnected += OnSessionOpen;
            wsServer.NewMessageReceived += OnMessage;
            wsServer.NewDataReceived += OnData;
            wsServer.SessionClosed += OnSessionClose;
            wsServer.Start();

            //HTTP (Used when callbeck isn't usefull)
            listener = new HttpListener();
            StartNgrok();
            listener.Prefixes.Add("http://localhost:9000/api/");
            listener.Start();
            while (true)
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
                    Log.Exception("Exception handling request", e);
                }
            }
        }

        private void OnSessionClose(WebSocketSession session, CloseReason value)
        {
            Trace.WriteLine("OnSessionClose: " + value);
        }

        private void OnData(WebSocketSession session, byte[] value)
        {
            Trace.WriteLine("OnData: " + value);
        }

        private void OnMessage(WebSocketSession session, string value)
        {
            dynamic json = new ExpandoObject();
            try
            {
                dynamic socketmessage = JsonConvert.DeserializeObject<ExpandoObject>(value);
                switch (socketmessage.messageType)
                {
                    case (Int64)SocketMessageType.command:
                        OnCommand(session, socketmessage.message);
                        break;
                    default:
                        json.error = "The socket message type isn't handled";
                        session.Send(JsonConvert.SerializeObject(json));
                        break;
                }
            }
            catch (Exception e)
            {
                json.error = "Error on socket message parsing. Your socket message shouldn't be in the right format";
                json.e = e;
                session.Send(JsonConvert.SerializeObject(json));
            }
        }

        private void OnSessionOpen(WebSocketSession session)
        {
            Trace.WriteLine("OnSessionOpen: " + session);
        }

        public void OnUrlRequest(HttpListenerRequest req, HttpListenerResponse resp)
        {
            Log.Trace("[VISITED]" + req.Url.ToString());

            bltApiUrls.ForEach(bltApiUrl =>
            {
                Uri existingUrl =  new Uri("http://localhost:9000/api/" + bltApiUrl.url);
                
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
            SendJSON(resp, bltApiUrls);
        }

        private void OnNgrok(HttpListenerRequest req, HttpListenerResponse resp)
        {
            dynamic json = new ExpandoObject();
            try
            {
                dynamic ngrokObj = JsonConvert.DeserializeObject(GetNgrokUrl());
                json.status = "Success";
                json.ngrok = ngrokObj?.tunnels[0]?.public_url;
                SendJSON(resp, json);
            }
            catch(Exception e)
            {
                json.status = "Fail";
                json.error = e;
                SendJSON(resp, json);
            }
        }

        public void OnCommand(WebSocketSession session, dynamic message)
        {
            dynamic json = new ExpandoObject();
            json.expected = new ExpandoObject();
            json.actualParams = new ExpandoObject();
            json.identifiedUser = new ExpandoObject();

            string cmdName = null;
            string userId = null;
            string args = null;
            try
            {
                cmdName = message.cmd;
                userId = message.userId;
                args = message.args;
            }
            catch(Exception e)
            {

            }

            if (cmdName == null || userId == null)
            {
                json.error = "Missing variables";
                json.expected.cmd = "string";
                json.expected.userId = "string";
                json.expected.args = "string";
            }
            else
            {
                MainThreadSync.Run(() => {
                    var client = BLTModule.TwitchService?.GetClientFromClientId(userId);
                    var commandFound = false;

                    json.actualParams.cmd = cmdName;
                    json.actualParams.userId = userId;
                    json.actualParams.args = args;

                    //if client isn't found or if twitch service isn't running (for example in main game menu)
                    if (client == null)
                    {
                        json.error = "Can't find a twitch user with this id OR twitch service isn't running";
                    }
                    else
                    {
                        json.identifiedUser = client;
                        commandFound = (bool)(BLTModule.TwitchService?.TestCommand(cmdName, client.DisplayName, args));
                        //if command isn't found
                        if (commandFound == false)
                        {
                            json.error = "Can't find this command";
                        }
                    }
                });
            }

            session.Send(JsonConvert.SerializeObject(json));
        }

        public void OnCommands(HttpListenerRequest req, HttpListenerResponse resp)
        {
            SendJSON(resp, settings.EnabledCommands.ToArray());
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

        public void StartNgrok()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "./../../Modules/BannerlordTwitch/bin/Win64_Shipping_Client/api/ngrok.exe";
            proc.StartInfo.Arguments = "http 9000 -host-header=\"localhost:9000\"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
        }

        public string GetNgrokUrl()
        {
            var requestHttps = (HttpWebRequest)WebRequest.Create("http://localhost:4040/api/tunnels");
            requestHttps.Method = "GET";
            var response = requestHttps.GetResponse();

            Stream receiveStream = response.GetResponseStream();

            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            return readStream.ReadToEnd();
        }
    }
}