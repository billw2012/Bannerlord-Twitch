using System;

namespace BannerlordTwitch.Rewards
{
    public abstract class ActionAndHandlerBase : IAction, ICommandHandler
    {
        protected virtual Type ConfigType => null;

        Type IAction.ActionConfigType => ConfigType;
        void IAction.Enqueue(ReplyContext context, object config)
        {
            ExecuteInternal(context, config, 
                s => RewardManager.NotifyComplete(context, s), 
                s => RewardManager.NotifyCancelled(context, s));
        }

        Type ICommandHandler.HandlerConfigType => ConfigType;
        void ICommandHandler.Execute(ReplyContext context, object config)
        {
            ExecuteInternal(context, config, 
                s => RewardManager.SendReply(context, s), 
                s => RewardManager.SendReply(context, s));
        }

        protected abstract void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure);
    }
}