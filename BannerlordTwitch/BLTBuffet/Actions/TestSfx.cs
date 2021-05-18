using System;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace BLTBuffet
{
    [UsedImplicitly]
    public class TestSfx : ICommandHandler
    {
        public void Execute(ReplyContext context, object config)
        {
            if (!string.IsNullOrEmpty(context.Args) && Agent.Main != null)
            {
                Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(context.Args), Agent.Main.AgentVisuals.GetGlobalFrame().origin, false, true, Agent.Main.Index, -1);
            }
        }

        public Type HandlerConfigType => null;
    }
}