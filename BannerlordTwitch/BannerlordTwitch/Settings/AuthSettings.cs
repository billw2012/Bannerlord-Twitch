using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Library;

namespace BannerlordTwitch
{
    [UsedImplicitly]
    public class AuthSettings
    {
        public string AccessToken { get; set; }
        public string ClientID { get; set; }
        public string BotAccessToken { get; set; }
        public string BotMessagePrefix { get; set; }
        public bool DebugSpoofAffiliate { get; set; }
        
        public string NeocitiesUsername { get; set; }
        
        public string NeocitiesPassword { get; set; }

        public string DocsTitle { get; set; } = "Bannerlord Twitch Viewer Guide";
        
        public string DocsIntroduction { get; set; } 
            = "Bannerlord Twitch (BLT) is a Twitch Integration mod for Mount & Blade II: Bannerlord.<br>" +
              "As a viewer you can use channel point rewards (if available), and chat commands to interact with the " +
              "game while the streamer is playing.<br>The primary feature of BLT is allowing you to 'adopt' a hero " +
              "in the game, i.e. a character in the game will be assigned to you, given your Twitch name, and be " +
              "available for you to interact with further.<br>" +
              "Some examples of things you can do with your hero include 'summoning' your hero into battles that the " +
              "streamer is taking part in, joining viewer tournaments, upgrading equipment, selecting your heroes " +
              "'class'.<br>" +
              "If you aren't sure where to start, begin by using the channel point redemption 'Adopt a Hero'.";
        
        private static PlatformFilePath AuthFilePath => FileSystem.GetConfigPath("Bannerlord-Twitch-Auth.yaml");

        public static AuthSettings Load()
        {
            return !FileSystem.FileExists(AuthFilePath) 
                ? null 
                : YamlHelpers.Deserialize<AuthSettings>(FileSystem.GetFileContentString(AuthFilePath));
        }

        public static void Save(AuthSettings authSettings)
        {
            FileSystem.SaveFileString(AuthFilePath, YamlHelpers.Serialize(authSettings));
        }
    }
}