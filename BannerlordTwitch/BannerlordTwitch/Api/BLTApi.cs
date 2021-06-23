using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using BannerlordTwitch;
using BannerlordTwitch.Api;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using Fleck;
using Newtonsoft.Json;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Internal;

namespace BannerlordApi
{
    public class BLTApi
    {
        private static HttpListener listener;
        private readonly Settings settings = Settings.Load();
        private static WebSocketServer wsServer; 
        
        public List<Tuple<string, IWebSocketConnection>> commandAwaitingResponse = new List<Tuple<string, IWebSocketConnection>>();

        public BLTApi()
        {
            _ = RunServerAsync();
        }

        public async Task RunServerAsync()
        {
            Log.Trace("Api server starting");

            var server = new WebSocketServer("ws://0.0.0.0:8431");
            server.Certificate = new X509Certificate2("./../../Modules/BannerlordTwitch/bin/Win64_Shipping_Client/api/certificate.pfx");
            server.Start(socket =>
            {
                socket.OnOpen = () => Trace.WriteLine("Open a socket");
                socket.OnClose = () => Trace.WriteLine("Close a socket");
                socket.OnMessage = message => OnMessage(socket, message);
            });

            //for now it's better to run it manually. TODO: add connection window to connect account to ngrok
            //StartNgrok(443);
        }

        private void OnSessionClose(IWebSocketConnection socket, CloseReason value)
        {
            Trace.WriteLine("OnSessionClose: " + value);
        }

        private void OnMessage(IWebSocketConnection socket, string value)
        {
            Trace.WriteLine("OnMessage: " + value);
            dynamic json = new ExpandoObject();
            try
            {
                dynamic socketmessage = JsonConvert.DeserializeObject<ExpandoObject>(value);
                switch (socketmessage.messageType)
                {
                    case (Int64)SocketMessageType.command:
                        OnCommand(socket, socketmessage.message);
                        break;
                    case (Int64)SocketMessageType.commands:
                        OnCommands(socket, socketmessage.message);
                        break;
                    case (Int64)SocketMessageType.ngrok:
                        OnNgrok(socket, socketmessage.message);
                        break;
                    default:
                        json.error = "The socket message type isn't handled";
                        socket.Send(JsonConvert.SerializeObject(json));
                        break;
                }
            }
            catch (Exception e)
            {
                json.error = "Error on socket message parsing. Your socket message shouldn't be in the right format";
                json.e = e;
                socket.Send(JsonConvert.SerializeObject(json));
            }
        }

        private void OnSessionOpen(IWebSocketConnection session)
        {
            Trace.WriteLine("OnSessionOpen: " + session);
        }

        private void OnNgrok(IWebSocketConnection session, dynamic message)
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

        public void OnCommand(IWebSocketConnection session, dynamic message)
        {
            dynamic json = new ExpandoObject();
            json.expected = new ExpandoObject();
            json.actualParams = new ExpandoObject();
            json.identifiedUser = new ExpandoObject();
            json.expected.cmd = "string";
            json.expected.userId = "string";
            json.expected.args = "string";

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
                //session.Send(JsonConvert.SerializeObject(json));
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

                    //session.Send(JsonConvert.SerializeObject(json));
                });
            }
        }

        public void OnCommands(IWebSocketConnection session, dynamic message)
        {
            session.Send(JsonConvert.SerializeObject(settings.EnabledCommands.ToArray()));
        }

        public void StartNgrok(int port)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "./../../Modules/BannerlordTwitch/bin/Win64_Shipping_Client/api/ngrok.exe";
            proc.StartInfo.Arguments = "http " + port;
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
        public IWebSocketConnection GetSocketAwaitingResponseFor(string displayName)
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