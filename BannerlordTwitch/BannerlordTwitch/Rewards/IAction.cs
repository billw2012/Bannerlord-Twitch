using System;

namespace BannerlordTwitch.Rewards
{
    public interface IAction
    {
        void Enqueue(ReplyContext context, object config);
        Type ActionConfigType { get; }
    }
}