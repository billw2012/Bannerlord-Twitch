using System;

namespace BannerlordTwitch.Rewards
{
    public abstract class ActionHandlerBase : IRewardHandler, ICommandHandler
    {
        protected virtual Type ConfigType => null;

        Type IRewardHandler.RewardConfigType => ConfigType;
        void IRewardHandler.Enqueue(ReplyContext context, object config)
        {
            ExecuteInternal(context, config, 
                s => ActionManager.NotifyComplete(context, s), 
                s => ActionManager.NotifyCancelled(context, s));
        }

        Type ICommandHandler.HandlerConfigType => ConfigType;
        void ICommandHandler.Execute(ReplyContext context, object config)
        {
            ExecuteInternal(context, config, 
                s => ActionManager.SendReply(context, s), 
                s => ActionManager.SendReply(context, s));
        }

        protected abstract void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure);
    }
}