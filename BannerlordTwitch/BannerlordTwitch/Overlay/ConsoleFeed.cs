using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using JetBrains.Annotations;

namespace BLTOverlay
{
    public static class ConsoleFeedOverlayControl
    {
        public static void Register()
        {
            BLTOverlay.Register("console", 0, @"
#bltconsole-container {
    position: absolute;
    left: 10px;
    bottom: 20px;
}

#bltconsole {
    list-style: none;
    padding: 0;
}

.bltconsole-text {
    filter: drop-shadow(0px 0px 2px black) drop-shadow(0px 0px 5px black);
    margin: 0;
}

", @"
<div id=""bltconsole-container"">
<ul id=""bltconsole"">
    <li class=""bltconsole-entry"" v-for=""item in items"" :key=""item.id"">
        <p class=""bltconsole-text"">{{item.message}}</p>
    </li>
</ul>
</div> 
", @"
$(function () {
    var bltconsole = new Vue({
        el: '#bltconsole',
        data: {items: []}
    });

    //Set the hubs URL for the connection
    $.connection.hub.url = ""$url_root$/signalr"";

    // Declare a proxy to reference the hub.
    const consoleFeedHub = $.connection.consoleFeedHub;

    // Create a function that the hub can call to broadcast messages.
    consoleFeedHub.client.addMessage = function (message) {
        bltconsole.items.push(message);
        console.log(message);
    };
    // Start the connection.
    $.connection.hub.start().done(function () {
        console.log('BLT Console Hub started');
    });
});
");
        }
    }

    public class ConsoleFeedHub : Hub
    {
        private static int Id = 0;

        private class Message
        {
            [UsedImplicitly]
            public int id = ++Id;
            [UsedImplicitly]
            public string message;
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
        
        public static void SendMessage(string message)
        {
            var newMsg = new Message {message = message};
            lock(messages) messages.Add(newMsg);

            GlobalHost.ConnectionManager.GetHubContext<ConsoleFeedHub>()
                .Clients.All.addMessage(newMsg);
        }
    }
}