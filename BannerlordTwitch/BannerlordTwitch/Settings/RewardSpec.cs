using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using BannerlordTwitch.Localization;
using JetBrains.Annotations;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BannerlordTwitch
{
    [LocDescription("{=glIst7fu}The Twitch specific part of the channel points reward specification")]
    [UsedImplicitly]
    public class RewardSpec
    {
        [LocDisplayName("{=EJDKdXPV}Title"),
         LocDescription("{=b8OBGhIk}The title of the reward"),
         PropertyOrder(1), UsedImplicitly]
        public LocString Title { get; set; } = "{=6ORulLUR}Reward Title Here";
        
        [LocDisplayName("{=Obz1yM6k}Disable Automatic Fulfillment"), 
         LocDescription("{=Kunfq4KA}Whether the reward will automatically be set to fulfilled once completed in game. If you set this to true then the redemptions that successfully complete in game will stay in your redemption queue. This is useful if you are worried about people losing points if the game crashes, or you reload an older save."), 
         PropertyOrder(1), UsedImplicitly]
        public bool DisableAutomaticFulfillment { get; set; }

        [LocDisplayName("{=e9jiFSF4}Prompt"),
         LocDescription("{=yXhrzrGA}Description / prompt"),
         PropertyOrder(2), UsedImplicitly]
        public LocString Prompt { get; set; } = string.Empty;
        
        [LocDisplayName("{=LnQoMDLT}Cost"), 
         LocDescription("{=dvYTG9UH}The cost of the reward"),
         Range(1, int.MaxValue),
         PropertyOrder(3), UsedImplicitly]
        public int Cost { get; set; } = 100;

        [LocDisplayName("{=oi2yz7aS}Is Enabled"), 
         LocDescription("{=iCzROk3u}Is the reward currently enabled, if false the reward won’t show up to viewers."), 
         PropertyOrder(4), UsedImplicitly]
        public bool IsEnabled { get; set; } = true;

        [Browsable(false)]
        public string BackgroundColorText { get; set; }
        
        [LocDisplayName("{=YzP57nUK}Background Color"), 
         LocDescription("{=Bg62Iggr}Custom background color for the reward"),
         PropertyOrder(5), UsedImplicitly]
        [YamlIgnore]
        public Color BackgroundColor
        {
            get => (Color) (ColorConverter.ConvertFromString(BackgroundColorText ?? $"#FF000000") ?? Colors.Black);
            set => BackgroundColorText = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        }

        [LocDisplayName("{=9aESWqlM}Is User Input Required"), 
         LocDescription("{=XqPRNwVc}Does the user need to enter information when redeeming the reward. If this is true the Prompt will be shown."), 
         PropertyOrder(6), UsedImplicitly]
        public bool IsUserInputRequired { get; set; }

        [LocDisplayName("{=qAxLr567}Max Per Stream"),
         LocCategory("Limits", "{=1lHWj3nT}Limits"), 
         LocDescription("{=Ho1k72DC}The maximum number per stream, defaults to unlimited"), 
         DefaultValue(null), PropertyOrder(7), UsedImplicitly]
        public int? MaxPerStream { get; set; }

        [LocDisplayName("{=V5NnvzZH}Max Per User Per Stream"),
         LocCategory("Limits", "{=1lHWj3nT}Limits"), 
         LocDescription("{=cdWQe1En}The maximum number per user per stream, defaults to unlimited"), 
         DefaultValue(null), PropertyOrder(8), UsedImplicitly]
        public int? MaxPerUserPerStream { get; set; }

        [LocDisplayName("{=Rulwnhfz}Global Cooldown Seconds"),
         LocCategory("Limits", "{=1lHWj3nT}Limits"), 
         LocDescription("{=yJRKVszb}The cooldown in seconds, defaults to unlimited"), 
         DefaultValue(null), PropertyOrder(9), UsedImplicitly]
        public int? GlobalCooldownSeconds { get; set; }

        private static string WebColor(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        public CreateCustomRewardsRequest GetTwitchSpec() =>
            new()
            {
                Title = Title?.ToString() ?? string.Empty,
                Cost = Cost,
                IsEnabled = IsEnabled,
                BackgroundColor = WebColor(BackgroundColor),
                IsUserInputRequired = IsUserInputRequired,
                Prompt = Prompt?.ToString() ?? string.Empty,
                // as we are performing the redemption we don't want to skip the queue
                ShouldRedemptionsSkipRequestQueue = false,
                IsGlobalCooldownEnabled = GlobalCooldownSeconds.HasValue,
                GlobalCooldownSeconds = GlobalCooldownSeconds,
                IsMaxPerStreamEnabled = MaxPerStream.HasValue,
                MaxPerStream = MaxPerStream,
                IsMaxPerUserPerStreamEnabled = MaxPerUserPerStream.HasValue,
                MaxPerUserPerStream = MaxPerUserPerStream,
            };

        public override string ToString() => Title?.ToString() ?? string.Empty;
    }
}