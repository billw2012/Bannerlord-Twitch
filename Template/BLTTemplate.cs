using System;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using TaleWorlds.MountAndBlade;

namespace BLTTemplate
{
    public class BLTTemplateModule : MBSubModuleBase
    {
        public const string Name = "BLTTemplate";
        public const string Ver = "1.0.0";

        public BLTTemplateModule()
        {
            RewardManager.RegisterAll(typeof(BLTTemplateModule).Assembly);
        }
    }

    [Desc("Example BLT action, with custom config")]
    public class BLTTemplateAction : IAction
    {
        public class Config
        {
            [Desc("An example value")]
            public int Value;
        }
        
        public void Enqueue(Guid redemptionId, string message, string userName, object config)
        {
            var settings = (Config) config;
            RewardManager.NotifyComplete(redemptionId);
        }

        public Type ActionConfigType => typeof(Config);
    }
    
    [Desc("Example BLT command with custom config")]
    public class BLTTemplateHandler : IHandler
    {
        public class Config
        {
            [Desc("An example value")]
            public int Value;
        }
        
        public void Execute(string args, CommandMessage message, object config)
        {
            var settings = (Config) config;
            if(message.IsSubscriber)
            {
                RewardManager.SendReply(message.ReplyId, $"Command was executed with value {settings.Value}!");
            }
            else
            {
                RewardManager.SendReply(message.ReplyId, $"Only available to subscribers!");
            }
        }

        public Type HandlerConfigType => typeof(Config);
    }

    [Desc("Example BLT action and handler combined, with custom config")]
    [UsedImplicitly]
    public class BLTTemplateCombined : IAction, IHandler
    {
        public class Config
        {
            [Desc("An example value")]
            public int Value;
        }

        public Type ActionConfigType => typeof(Config);
        public Type HandlerConfigType => typeof(Config);

        public void Enqueue(Guid redemptionId, string message, string userName, object config)
        {
            var (success, reply) = Impl(message, (Config) config);
            if (success)
            {
                RewardManager.NotifyComplete(redemptionId, reply);
            }
            else
            {
                RewardManager.NotifyCancelled(redemptionId, reply);
            }
            
        }
        
        public void Execute(string args, CommandMessage message, object config)
        {
            var (success, reply) = Impl(args, (Config) config);
            if (success)
            {
                RewardManager.SendReply(message.ReplyId, $"Success: {reply}");
            }
            else
            {
                RewardManager.SendReply(message.ReplyId, $"Failure: {reply}");
            }
        }

        private (bool success, string reply) Impl(string args, Config config)
        {
            return (true, "message to viewer");
        }
    }
}