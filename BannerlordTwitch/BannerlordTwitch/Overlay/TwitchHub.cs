using System.Collections.Generic;
using System.Threading.Tasks;
using BannerlordTwitch.Annotations;
using Microsoft.AspNet.SignalR;
using BannerlordTwitch.Util;

namespace BLTOverlay
{
    public class TwitchHub : Hub
    {
        private class User
        {
            [UsedImplicitly]
            public string color;

            public string displayName;
        }

        private static readonly Dictionary<string, User> users = new();

        public override Task OnConnected()
        {
            Refresh();
            return base.OnConnected();
        }

        [UsedImplicitly]
        public void Refresh()
        {
            lock (users)
            {
                foreach ((string userName, var userData) in users)
                {
                    Clients.Caller.addUser(userName, userData);
                }
            }
        }

        public static void AddUser(string displayName, string colorHex)
        {
            lock(users)
            {
                var userData = new User { color = colorHex, displayName = displayName};
                users[displayName.ToLower()] = userData;
                GlobalHost.ConnectionManager.GetHubContext<TwitchHub>()
                    .Clients.All.addUser(displayName, userData);
            }
        }
    }
}