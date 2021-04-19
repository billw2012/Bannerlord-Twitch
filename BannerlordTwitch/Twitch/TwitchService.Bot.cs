using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace BannerlordTwitch
{
    internal partial class TwitchService
    {
        private class Bot
        {
            private TwitchClient client;
            private string channel;
            private readonly Settings settings;
	    
            public Bot(string channel, Settings settings)
            {
                this.settings = settings;
                this.channel = channel;
                
                Connect();
            }

            private void Connect()
            {
                var credentials = new ConnectionCredentials(channel, settings.BotAccessToken, disableUsernameCheck: true);
                var clientOptions = new ClientOptions
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30)
                };
                var customClient = new WebSocketClient(clientOptions);
                client = new TwitchClient(customClient);
                client.Initialize(credentials, channel);

                client.OnLog += Client_OnLog;
                client.OnJoinedChannel += Client_OnJoinedChannel;
                client.OnMessageReceived += Client_OnMessageReceived;
                //client.OnWhisperReceived += Client_OnWhisperReceived;
                // client.OnNewSubscriber += Client_OnNewSubscriber;
                client.OnConnected += Client_OnConnected;

                client.Connect();
            }

            public List<string> FormatMessage(params string[] msg)
            {
                const string space = " ░▓█▓░ ";// " ▄▓▄▓▄ ";
                var parts = new List<string>();
                string currPart = msg.First();
                foreach (var msgPart in msg.Skip(1))
                {
                    if (currPart.Length + space.Length + msgPart.Length > 450)
                    {
                        parts.Add(currPart);
                        currPart = msgPart;
                    }
                    else
                    {
                        currPart += space + msgPart;
                    }
                }
                parts.Add(currPart);
                return parts;  // string.Join(space, msg);
            }
            
            public void SendChat(params string[] msg)
            {
                // if (!client.IsConnected)
                // {
                //     client.Connect();
                // }
                if (client.IsConnected)
                {
                    try
                    {
                        var parts = FormatMessage(msg);
                        foreach (var part in parts)
                        {
                            client.SendMessage(channel, (settings.BotMessagePrefix ?? "[BLT] ") + part);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to send chat: {e.Message}");
                    }
                }
            }

            public void SendReply(string replyId, params string[] msg)
            {
                // if (!client.IsConnected)
                // {
                //     client.Connect();
                // }
                if (client.IsConnected)
                {
                    try
                    {
                        var parts = FormatMessage(msg);
                        foreach (var part in parts)
                        {
                            client.SendReply(channel, replyId, (settings.BotMessagePrefix ?? "[BLT] ") + part);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to send reply: {e.Message}");
                    }
                }
            }

            private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
            {
                Log.Trace($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
            }

            private void Client_OnConnected(object sender, OnConnectedArgs e)
            {
                Log.Screen($"{e.BotUsername} connected to {e.AutoJoinChannel}");

                var cts = new CancellationTokenSource();

                Task.Factory.StartNew(() => {
                    while (!cts.IsCancellationRequested)
                    {
                        MainThreadSync.Run(() =>
                        {
                            if (!client.IsConnected || client.JoinedChannels.Count == 0)
                            {
                                client.Disconnect();
                                cts.Cancel();
                                Connect();
                            }
                        });
                        Task.Delay(TimeSpan.FromSeconds(60), cts.Token).Wait();
                    }
                }, TaskCreationOptions.LongRunning);
            }

            private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
            {
                Log.Screen($"BLT bot has joined {e.Channel}");
                SendChat("BLT bot reporting for duty!", "Type !help for command list");
            }
            
            private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
            {
                var msg = e.ChatMessage.Message;
                if (msg.StartsWith("!"))
                {
                    HandleChatBoxMessage(msg.TrimStart('!'), e.ChatMessage.Username, e.ChatMessage.Id);
                }
            }

            private void HandleChatBoxMessage(string msg, string userName, string replyId)
            {
                var parts = msg.Split(' ');
                if (parts[0] == "help")
                {
                    BLTModule.TwitchService.ShowCommandHelp(replyId);
                }
                else
                {
                    var cmd = settings.Commands.FirstOrDefault(c => c.Cmd == parts[0]);
                    if (cmd != null)
                    {
                        RewardManager.Command(cmd.Handler, msg.Substring(parts[0].Length).Trim(), userName, replyId, cmd.Config);
                    }
                }
            }

            // private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
            // {
            //     //HandleChatBoxMessage(e.WhisperMessage.Message, e.WhisperMessage.Username, e.WhisperMessage.MessageId);
            // }
            
            // private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
            // {
            //     if (e.Subscriber.SubscriptionPlan == TwitchLib.Client.Enums.SubscriptionPlan.Prime)
            //         client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
            //     else
            //         client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
            // }
        }
    }
}