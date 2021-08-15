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

        public string DocsTitle { get; set; } = "{=ctAF1ghX}".Translate();
        
        public string DocsIntroduction { get; set; } 
            = "{=rElL5v6I}".Translate();
        
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