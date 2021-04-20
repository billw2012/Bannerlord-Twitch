using System;
using Newtonsoft.Json.Linq;

namespace BannerlordTwitch.Rewards
{
    public interface IRedemptionAction
    {
        void Enqueue(Guid redemptionId, string message, string userName, JObject config);
    }
}