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

.bltconsole-items-t-enter, .bltconsole-items-t-leave-to {
    opacity: 0;
    background: #7000;
    transform: translateY(50px);
}

.bltconsole-items-t-leave-active {
    position: absolute;
}

.bltconsole-entry {
    transition: all 0.2s;
}

.bltconsole-text-style-general {
    color: white;
}

.bltconsole-text-style-response {
    color: white;
}

.bltconsole-text-style-system {
    color: cyan;
}

.bltconsole-text-style-internal {
    color: gray;
    font-size: 80%;
}

.bltconsole-text-style-battle {
    color: yellow;
}

.bltconsole-text-style-event {
    color: white;
}

.bltconsole-text-style-fail {
    color: red;
    font-weight: bold;
}

.bltconsole-text-style-critical {
    color: red;
}

", @"
<div id='bltconsole-container' class='drop-shadow'>
    <div id='bltconsole-items'>
        <transition-group name='bltconsole-items-t' tag='div'>
            <div class='bltconsole-entry' v-for='item in items' v-bind:key='item.id'>
                <div class='bltconsole-text' v-bind:class=""'bltconsole-text-style-' + item.style"">{{item.message}}</div>
            </div>
        </transition-group>
    </div>
</div> 
", @"
$(function () {
    const bltConsole = new Vue({
        el: '#bltconsole-container',
        data: { items: [] }
    });
    $.connection.hub.url = '$url_root$/signalr';
    $.connection.hub.error(function (error) {
        console.log('Overlay error: ' + error);
        // bltConsole.items.push({ id: -1, message: 'Overlay error: ' + error, style: 'fail' });
    });
    $.connection.hub.starting(function () {
        console.log('Overlay starting');
        bltConsole.items.push({ id: -1, message: 'Overlay starting', style: 'internal' });
    });
    $.connection.hub.connectionSlow(function () {
        console.log('Overlay connectionSlow');
        bltConsole.items.push({ id: -1, message: 'Overlay connectionSlow', style: 'internal' });
    });
    $.connection.hub.reconnecting(function () {
        console.log('Overlay reconnecting');
        bltConsole.items.push({ id: -1, message: 'Overlay reconnecting', style: 'internal' });
    });
    $.connection.hub.reconnected(function () {
        console.log('Overlay reconnected');
        bltConsole.items.push({ id: -1, message: 'Overlay reconnected', style: 'internal' });
    });
    $.connection.hub.disconnected(function () {
        console.log('Overlay disconnected');
        bltConsole.items.push({ id: -1, message: 'Overlay disconnected', style: 'internal' });
    });
    const consoleFeedHub = $.connection.consoleFeedHub;
    consoleFeedHub.client.addMessage = function (message) {
        bltConsole.items.push(message);
        if(bltConsole.items.length > 100)
        {
            bltConsole.items.shift();
        }
        console.log(message);
    };
    $.connection.hub.start().done(function () {
        console.log('BLT Console Hub connected');
    }).fail(function(){ 
        bltConsole.items.push({ id: -1, message: 'BLT Console Hub started could not connect', style: 'fail' });
        console.log('BLT Console Hub started could not connect'); 
    });
});
");
        }
    }
}