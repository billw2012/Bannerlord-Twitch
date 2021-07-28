using Microsoft.AspNet.SignalR;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace BLTOverlay
{
    public class ConsoleFeedHub : Hub
    {
        private static int Id = 0;

        private class Message
        {
            [UsedImplicitly]
            public int id = ++Id;
            [UsedImplicitly]
            public string message;
            [UsedImplicitly] 
            public string style;
        }

        private static readonly List<Message> messages = new();

        public override Task OnConnected()
        {
            Refresh();
            return base.OnConnected();
        }

        [UsedImplicitly]
        public void Refresh()
        {
            lock (messages)
            {
                foreach (var msg in messages)
                {
                    Clients.Caller.addMessage(msg);
                }
            }
        }
        
        public static void SendMessage(string message, string style)
        {
            var newMsg = new Message { message = message, style = style };
            lock(messages)
            {
                messages.Add(newMsg);
                if (messages.Count > 100)
                {
                    messages.RemoveAt(0);
                }
            }

            GlobalHost.ConnectionManager.GetHubContext<ConsoleFeedHub>()
                .Clients.All.addMessage(newMsg);
        }
        
        private static string GetContentPath(string fileName) => Path.Combine(
            Path.GetDirectoryName(typeof(ConsoleFeedHub).Assembly.Location) ?? ".",
 "Overlay", "ConsoleFeed", fileName);
        private static string GetContent(string fileName) => File.ReadAllText(GetContentPath(fileName));
        
        public static void Register()
        {
            BLTOverlay.Register("console", 0, 
                GetContent("ConsoleFeed.css"), 
                GetContent("ConsoleFeed.html"), 
                GetContent("ConsoleFeed.js"));
        }
    }
}