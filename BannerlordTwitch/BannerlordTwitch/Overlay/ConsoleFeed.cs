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
        
        public static void Register()
        {
            BLTOverlay.Register("console", 0, @"
#bltconsole-container {
}
#bltconsole-items {
}
.bltconsole-text {
    margin: 0;
}

", @"
<div id='bltconsole-container' class='drop-shadow'>
    <div id='bltconsole-items'>
        <div class='bltconsole-entry' v-for='item in items'>
            <p class='bltconsole-text'>{{item.message}}</p>
        </div>
    </div>
</div> 
", @"
$(function () {
    const bltConsole = new Vue({
        el: '#bltconsole-container',
        data: { items: [] }
    });
    $.connection.hub.url = '$url_root$/signalr';
    $.connection.hub.logging = true;
    const consoleFeedHub = $.connection.consoleFeedHub;
    consoleFeedHub.client.addMessage = function (message) {
        bltConsole.items.push(message);
        console.log(message);
    };
    $.connection.hub.start().done(function () {
        console.log('BLT Console Hub started');
    }).fail(function(){ 
        console.log('BLT Console Hub started could not Connect!'); 
    });
});
");
        }
    }
}