namespace BannerlordTwitch.Rewards
{
    public interface IConfig
    {
        void OnLoaded();
        void OnSaving();
        void OnEditing();
    }
}