using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BannerlordTwitch.Rewards
{
    public interface IAction
    {
        void Enqueue(Guid redemptionId, string message, string userName, object config);
        Type ActionConfigType { get; }
    }
}