namespace BannerlordTwitch.Rewards
{
    public interface IConfig
    {
        void OnLoaded(Settings settings);
        void OnSaving();
    }
}