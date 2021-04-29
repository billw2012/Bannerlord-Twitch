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
            private readonly string channel;
            private TwitchClient client;
            private readonly TwitchService twitchService;
            private readonly AuthSettings authSettings;
	    
            public Bot(string channel, AuthSettings authSettings, TwitchService twitchService)
            {
                this.twitchService = twitchService;
                this.authSettings = authSettings;
                this.channel = channel;
                
                Connect();
            }

            private void Connect()
            {
                var credentials = new ConnectionCredentials(channel, authSettings.BotAccessToken, disableUsernameCheck: true);
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
                client.OnConnected += Client_OnConnected;

                client.Connect();
            }

            private static IEnumerable<string> FormatMessage(params string[] msg)
            {
                const string space = " ░ "; // " ░▓█▓░ ";// " ▄▓▄▓▄ ";
                var parts = new List<string>();
                var currPart = msg.First();
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
                if (client.IsConnected)
                {
                    try
                    {
                        var parts = FormatMessage(msg);
                        foreach (var part in parts)
                        {
                            client.SendMessage(channel, (authSettings.BotMessagePrefix ?? "[BLT] ") + part);
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
                if (client.IsConnected)
                {
                    try
                    {
                        var parts = FormatMessage(msg);
                        foreach (var part in parts)
                        {
                            client.SendReply(channel, replyId, (authSettings.BotMessagePrefix ?? "[BLT] ") + part);
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
                Log.Screen($"bot has joined {e.Channel}");
                SendChat("bot reporting for duty!", "Type !help for command list");
            }

            private static CommandMessage GetCommandMessage(ChatMessage from) =>
                new()
                {
                    UserName = from.DisplayName, // DisplayName not UserName, as it has correct capitalization
                    ReplyId = from.Id,
                    Bits = from.Bits,
                    BitsInDollars = from.BitsInDollars,
                    SubscribedMonthCount = from.SubscribedMonthCount,
                    IsBroadcaster = from.IsBroadcaster,
                    IsHighlighted = from.IsHighlighted,
                    IsMe = from.IsMe,
                    IsModerator = from.IsModerator,
                    IsSkippingSubMode = from.IsSkippingSubMode,
                    IsSubscriber = from.IsSubscriber,
                    IsVip = from.IsVip,
                    IsStaff = from.IsStaff,
                    IsPartner = from.IsPartner
                };

            private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
            {
                var msg = e.ChatMessage.Message;
                if (msg.StartsWith("!"))
                {
                    HandleChatBoxMessage(msg.TrimStart('!'), GetCommandMessage(e.ChatMessage));
                }
            }

            private void HandleChatBoxMessage(string msg, CommandMessage commandMessage)
            {
                MainThreadSync.Run(() => {
                    string[] parts = msg.Split(' ');
                    if (parts[0] == "help")
                    {
                        BLTModule.TwitchService.ShowCommandHelp(commandMessage.ReplyId);
                    }
                    else
                    {
                        var cmd = twitchService.Settings.Commands.FirstOrDefault(c => c.Name == parts[0]);
                        if (cmd != null 
                            && (!cmd.ModOnly || commandMessage.IsModerator || commandMessage.IsBroadcaster)
                            && (!cmd.BroadcasterOnly || commandMessage.IsBroadcaster)
                            )
                        {
                            RewardManager.Command(cmd.Handler, msg.Substring(parts[0].Length).Trim(),
                                commandMessage, cmd.HandlerConfig);
                        }
                    }
                });
            }
        }
    }
}