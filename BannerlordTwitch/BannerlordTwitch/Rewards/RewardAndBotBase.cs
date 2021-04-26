using System;

namespace BannerlordTwitch.Rewards
{
    public abstract class ActionAndHandlerBase : IAction, ICommandHandler
    {
        protected virtual Type ConfigType => null;

        Type IAction.ActionConfigType => ConfigType;
        void IAction.Enqueue(Guid redemptionId, string args, string userName, object config)
        {
            ExecuteInternal(userName, args, config, 
                s => RewardManager.NotifyComplete(redemptionId, s), 
                s => RewardManager.NotifyCancelled(redemptionId, s));
        }

        Type ICommandHandler.HandlerConfigType => ConfigType;
        void ICommandHandler.Execute(string args, CommandMessage message, object config)
        {
            ExecuteInternal(message.UserName, args, config, 
                s => RewardManager.SendReply(message.ReplyId, s), 
                s => RewardManager.SendReply(message.ReplyId, s));
        }

        protected abstract void ExecuteInternal(string userName, string args, object config,
            Action<string> onSuccess,
            Action<string> onFailure);
    }
}