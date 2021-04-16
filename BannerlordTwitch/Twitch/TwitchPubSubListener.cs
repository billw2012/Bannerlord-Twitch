using System.Linq;
using System.Threading.Tasks;
using BannerlordTwitch.Util;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace BannerlordTwitch
{
    // https://twitchtokengenerator.com/
    // https://twitchtokengenerator.com/quick/AAYotwZPvU
    public class TwitchService
    {
        private static string ClientID = "gp762nuuoqcoxypju8c569th9wz7q5"; // From twitchtokengenerator
        //private static string SECRET = "x8we5hdwa2oq5rtv8mblddyp1p48b9";
        
        // public static class Secrets
        // {
        //     public static string OAuthToken = "23w6gwi7pmyh5mph8ew0d9aobjjwv6"; //A Twitch OAuth token which can be used to connect to the chat
        //     ///public static string RefreshToken = "234c2eqgwasw620ckt9f5idjz7mb4158q9sn5lbgf95wha5unr";
        //     //public static string USERNAME_FROM_OAUTH_TOKEN = "USERNAME_FROM_OAUTH_TOKEN"; //The username which was used to generate the OAuth token
        //     public static string ChannelIDFromOAuthToken; //The channel Id from the account which was used to generate the OAuth token
        // }
        
        private TwitchPubSub pubSub;
        private TwitchAPI api;
        private string channelId;

        public TwitchService(string authToken)
        {
            // To keep the Unity application active in the background, you can enable "Run In Background" in the player settings:
            // Unity Editor --> Edit --> Project Settings --> Player --> Resolution and Presentation --> Resolution --> Run In Background
            // This option seems to be enabled by default in more recent versions of Unity. An aditional, less recommended option is to set it in code:
            // Application.runInBackground = true;

            api = new TwitchAPI();
            //api.Settings.Secret = SECRET;
            api.Settings.ClientId = ClientID;

            api.Helix.Users.GetUsersAsync(accessToken: authToken).ContinueWith(t =>
            {
                MainThreadSync.Enqueue(() =>
                {
                    Util.Log.Info($"Channel ID {t.Result.Users.First().Id}");
                    channelId = t.Result.Users.First().Id;
                    // Create new instance of PubSub Client
                    pubSub = new TwitchPubSub();

                    // Subscribe to Events
                    //_pubSub.OnWhisper += OnWhisper;
                    pubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;
                    pubSub.OnRewardRedeemed += OnOnRewardRedeemed;

                    api.Helix.ChannelPoints.CreateCustomRewards(channelId, new CreateCustomRewardsRequest(), authToken);
                    
                    // Connect
                    pubSub.Connect();
                });
            });
        }

        private void OnOnRewardRedeemed(object sender, OnRewardRedeemedArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void OnPubSubServiceConnected(object sender, System.EventArgs e)
        {
            Log.Info("PubSubServiceConnected!");

#pragma warning disable 618
            // Obsolete warning disabled because no new version has yet been written!
            pubSub.ListenToRewards(channelId);
#pragma warning restore 618
        }
    }
}