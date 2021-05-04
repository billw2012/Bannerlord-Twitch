using System;

namespace BannerlordTwitch.Rewards
{
    public interface ICommandHandler
    {
        void Execute(ReplyContext context, object config);
        Type HandlerConfigType { get; }
    }
}