using System;

namespace BannerlordTwitch.Rewards
{
    public interface IAction
    {
        void Enqueue(Guid redemptionId, string message, string userName, object config);
        Type ActionConfigType { get; }
    }
}