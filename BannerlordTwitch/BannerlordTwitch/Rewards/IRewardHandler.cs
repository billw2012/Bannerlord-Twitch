using System;

namespace BannerlordTwitch.Rewards
{
    public interface IRewardHandler
    {
        void Enqueue(ReplyContext context, object config);
        Type ActionConfigType { get; }
    }
}