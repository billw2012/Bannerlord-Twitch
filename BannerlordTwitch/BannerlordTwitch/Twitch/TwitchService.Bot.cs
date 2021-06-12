using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace BannerlordTwitch
{
    internal partial class TwitchService
    {
        private class Bot : IDisposable
        {
            private readonly string channel;
            private TwitchClient client;
            private readonly AuthSettings authSettings;
            private string botUserName;

            public Bot(string channel, AuthSettings authSettings)
            {
                this.authSettings = authSettings;
                this.channel = channel;
                
                var api = new TwitchAPI();

                //api.Settings.Secret = SECRET;
                api.Settings.ClientId = authSettings.ClientID;
                api.Settings.AccessToken = authSettings.BotAccessToken;

                // Get the bot username
                api.Helix.Users.GetUsersAsync(accessToken: authSettings.BotAccessToken).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Log.LogFeedSystem($"Bot connection failed: {t.Exception?.Message}");
                        return;
                    }
                    var user = t.Result.Users.First();

                    Log.Info($"Bot user is {user.Login}");
                    botUserName = user.Login;
                    Connect();
                });
            }

            private void Connect()
            {
                var credentials = new ConnectionCredentials(botUserName, authSettings.BotAccessToken, disableUsernameCheck: true);
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
                client.OnDisconnected += Client_OnDisconnected;
                // client.OnWhisperReceived += Client_OnWhisperReceived;

                client.Connect();
            }

            private static IEnumerable<string> FormatMessage(params string[] msg)
            {
                const string space = " ░ "; // " ░▓█▓░ ";// " ▄▓▄▓▄ ";
                var parts = new List<string>();
                string currPart = msg.First();
                foreach (string msgPart in msg.Skip(1))
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
                        foreach (string part in parts)
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
            
            public void SendChatReply(string userName, params string[] msg)
            {
                if (client.IsConnected)
                {
                    try
                    {
                        var parts = FormatMessage(msg);
                        foreach (string part in parts)
                        {
                            client.SendMessage(channel, $"{authSettings.BotMessagePrefix ?? "[BLT] "}@{userName} {part}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to send reply: {e.Message}");
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
                        foreach (string part in parts)
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

            public void SendWhisper(string userName, params string[] msg)
            {
                if (client.IsConnected)
                {
                    try
                    {
                        var parts = FormatMessage(msg);
                        foreach (string part in parts)
                        {
                            client.SendWhisper(userName, (authSettings.BotMessagePrefix ?? "[BLT] ") + part);
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

            private bool autoReconnect = true;

            private void Client_OnConnected(object sender, OnConnectedArgs e)
            {
                Log.LogFeedSystem($"Bot connected");

                // disconnectCts = new CancellationTokenSource();
                // Task.Factory.StartNew(() => {
                //     while (!disconnectCts.IsCancellationRequested)
                //     {
                //         MainThreadSync.Run(() =>
                //         {
                //             if (!client.IsConnected || client.JoinedChannels.Count == 0)
                //             {
                //                 client.Disconnect();
                //                 disconnectCts.Cancel();
                //                 Connect();
                //             }
                //         });
                //         Task.Delay(TimeSpan.FromSeconds(15), disconnectCts.Token).Wait();
                //     }
                // }, TaskCreationOptions.LongRunning);
            }
            
            private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
            {
                Log.LogFeedSystem($"Bot disconnected");
                if (autoReconnect)
                {
                    Connect();
                }
            }


            private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
            {
                Log.LogFeedSystem($"@{e.BotUsername} has joined channel {e.Channel}");
                SendChat("bot reporting for duty!", "Type !help for command list");
            }

            private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
            {
                string msg = e.ChatMessage.Message;
                if (msg.StartsWith("!"))
                {
                    HandleChatBoxMessage(msg.TrimStart('!'), e.ChatMessage);
                }
            }

            // // private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
            // {
            //     HandleChatBoxMessage(e.WhisperMessage.Message, ReplyContext.FromWhisper(e.WhisperMessage));
            // }

            private void HandleChatBoxMessage(string msg, ChatMessage chatMessage)
            {
                MainThreadSync.Run(() =>
                {
                    string[] parts = msg.Split(' ');
                    if (parts[0] == "help")
                    {
                        BLTModule.TwitchService?.ShowCommandHelp();
                    }
                    else
                    {
                        string cmdName = parts[0];
                        string args = msg.Substring(cmdName.Length).Trim();
                        BLTModule.TwitchService?.ExecuteCommand(cmdName, chatMessage, args);
                    }
                });
            }

            private void ReleaseUnmanagedResources()
            {
                autoReconnect = false;
                client?.Disconnect();
                client = null;
            }

            public void Dispose()
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            ~Bot()
            {
                ReleaseUnmanagedResources();
            }
        }
    }
}