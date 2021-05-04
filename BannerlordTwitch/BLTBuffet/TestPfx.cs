using System;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTBuffet
{
    [UsedImplicitly]
    public class TestPfx : ICommandHandler
    {
        private BoneAttachments active;
        public void Execute(ReplyContext context, object config)
        {
            if (Agent.Main == null)
            {
                return;
            }
            if (active != null)
            {
                CharacterEffect.RemoveAgentEffects(active);
                active = null;
            }
            if (!string.IsNullOrEmpty(context.Args))
            {
                active = CharacterEffect.CreateAgentEffects(Agent.Main, context.Args, 
                    MatrixFrame.Identity.Strafe(0.1f),
                    Game.Current.HumanMonster.HeadLookDirectionBoneIndex);
            }
        }

        public Type HandlerConfigType => null;
    }
}