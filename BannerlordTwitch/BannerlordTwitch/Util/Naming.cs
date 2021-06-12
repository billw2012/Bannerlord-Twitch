namespace BannerlordTwitch.Util
{
    public static class Naming
    {
        public const string To = "→";
        public const string Inc = "+";
        public const string Dec = "-";
        public const string Gold = "⦷";
        public const string XP = "xp";

        public static string NotEnoughGold(int need, int have) =>
            $"Not enough {Naming.Gold}: need {need}{Naming.Gold}, have {have}{Naming.Gold}!";
    }
}