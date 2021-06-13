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
using SuperSocket.SocketBase.Config;
using SuperWebSocket;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Internal;

namespace BannerlordApi
{
    public class BLTApi
    {
        private static HttpListener listener;
        private readonly Settings settings = Settings.Load();
        private static WebSocketServer wsServer;

        public List<Tuple<string, WebSocketSession>> commandAwaitingResponse = new List<Tuple<string, WebSocketSession>>();

        public BLTApi()
        {
            _ = RunServerAsync();
        }

        public async Task RunServerAsync()
        {
            Log.Trace("Api server starting");

            //SOCKET (Used when we have callback to do)
            //wss websocket work in local by creating a self signed certificate & do websocket request on wss://localhost:443. But if you try by ngrok, it's fail
            wsServer = new WebSocketServer();

            var m_Config = new ServerConfig
            {
                Port = 443,
                Ip = "Any",
                MaxConnectionNumber = 1000,
                Mode = SocketMode.Tcp,
                Name = "CustomProtocolServer",
                Certificate = new CertificateConfig
                {
                    FilePath = @"C:\certificates\certificate.pfx",
                    Password = ""
                },
                Security = "tls"
            };

            wsServer.Setup(m_Config);
            wsServer.NewSessionConnected += OnSessionOpen;
            wsServer.NewMessageReceived += OnMessage;
            wsServer.NewDataReceived += OnData;
            wsServer.SessionClosed += OnSessionClose;
            
            wsServer.Start();
            //StartNgrok(443);
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
            Trace.WriteLine("OnMessage: " + value);
            dynamic json = new ExpandoObject();
            try
            {
                dynamic socketmessage = JsonConvert.DeserializeObject<ExpandoObject>(value);
                switch (socketmessage.messageType)
                {
                    case (Int64)SocketMessageType.command:
                        OnCommand(session, socketmessage.message);
                        break;
                    case (Int64)SocketMessageType.ngrok:
                        OnNgrok(session, socketmessage.message);
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

        private void OnNgrok(WebSocketSession session, dynamic message)
        {
            dynamic json = new ExpandoObject();
            try
            {
                dynamic ngrokObj = JsonConvert.DeserializeObject(GetNgrokUrl());
                json.status = "Success";
                json.ngrok = ngrokObj?.tunnels[0]?.public_url;
                session.Send(JsonConvert.SerializeObject(json));
            }
            catch(Exception e)
            {
                json.status = "Fail";
                json.error = e;
                session.Send(JsonConvert.SerializeObject(json));
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
                        commandAwaitingResponse.Add(Tuple.Create(client.DisplayName, session));
                        commandFound = (bool)(BLTModule.TwitchService?.TestCommand(cmdName, client.DisplayName, args));
                    }
                });
            }

            session.Send(JsonConvert.SerializeObject(json));
        }

        public void OnCommands(WebSocketSession session, dynamic message)
        {
            session.Send(JsonConvert.SerializeObject(settings.EnabledCommands.ToArray()));
        }

        public void StartNgrok(int port)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "./../../Modules/BannerlordTwitch/bin/Win64_Shipping_Client/api/ngrok.exe";
            proc.StartInfo.Arguments = "tcp " + port;
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
        public WebSocketSession GetSocketAwaitingResponseFor(string displayName)
        {
            var tuple = commandAwaitingResponse.Find(tuple => tuple.Item1 == displayName);
            if (tuple != null)
            {
                commandAwaitingResponse.Remove(tuple);
                return tuple.Item2;
            }
            return null;
        }
    }
}