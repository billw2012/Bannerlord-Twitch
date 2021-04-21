using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BannerlordTwitch.Util;
using Newtonsoft.Json.Linq;
using TaleWorlds.Core;

namespace BannerlordTwitch.Rewards
{
    public static class RewardManager
    {
        private static readonly Dictionary<string, IRedemptionAction> actions = new();
        private static readonly Dictionary<string, IBotCommand> commands = new();
        
        public static void Init()
        {
            RegisterAll(typeof(RewardManager).Assembly);
        }

        public static void RegisterAll(Assembly assembly)
        {
            var redemptionActionTypes = assembly
                .GetTypes()
                .Where(t => typeof(IRedemptionAction).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var redemptionActionType in redemptionActionTypes)
            {
                RegisterAction((IRedemptionAction) Activator.CreateInstance(redemptionActionType));
            }

            var botCommands = assembly
                .GetTypes()
                .Where(t => typeof(IBotCommand).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var botCommandType in botCommands)
            {
                RegisterCommand((IBotCommand) Activator.CreateInstance(botCommandType));
            }
        }

        public static bool RegisterAction(IRedemptionAction action)
        {
            var id = action.GetType().Name;
            if (actions.ContainsKey(id))
            {
                Log.Error($"Reward Action {id} already registered, please choose another name");
                return false;
            }
            Log.Trace($"Registered Reward Action {id}");
            actions.Add(id, action);
            return true;
        }
        
        public static bool RegisterCommand(IBotCommand command)
        {
            var id = command.GetType().Name;
            if (commands.ContainsKey(id))
            {
                Log.Error($"Bot Command {id} already registered, please choose another name");
                return false;
            }
            Log.Trace($"Registered Bot Command {id}");
            commands.Add(id, command);
            return true;
        }

        public static JObject FindGlobalConfig(string id) => BLTModule.TwitchService.FindGlobalConfig(id);

        internal static void Command(string id, string args, CommandMessage commandMessage, JObject config)
        {
            if (commands.TryGetValue(id, out var cmdHandler))
            {
                try
                {
                    cmdHandler.Execute(args, commandMessage, config);
                }
                catch (Exception e)
                {
                    Log.ScreenCritical($"Command {id} failed with exception {e.Message}, game might be unstable now!");
                    Log.Error(e.ToString());
                }
            }
            else
            {
                Log.ScreenFail($"Command with id {id} couldn't be found, check Commands config");
            }
        }

        internal static bool Enqueue(string actionId, Guid redemptionId, string message, string userName,
            JObject config)
        {
            if (!actions.TryGetValue(actionId, out var action))
            {
                Log.Error($"Action with the id {actionId} doesn't exist");
                return false;
            }

            var st = new Stopwatch();
            st.Start();
            try
            {
                action.Enqueue(redemptionId, message, userName, config);
            }
            catch (Exception e)
            {
                Log.ScreenCritical($"Action {actionId} failed with exception {e.Message}, game might be unstable now!");
                Log.Error(e.ToString());
                NotifyCancelled(redemptionId, $"Error occurred while trying to process the redemption");
            }

            if (st.ElapsedMilliseconds > 5)
            {
                Log.Info($"Action {actionId} took {st.ElapsedMilliseconds}ms to Enqueue, this is too slow!");
            }

            return true;
        }

        public static void NotifyComplete(Guid id, string status = null)
        {
            BLTModule.TwitchService.RedemptionComplete(id, status);
        }
        
        public static void NotifyCancelled(Guid id, string reason = null)
        {
            BLTModule.TwitchService.RedemptionCancelled(id, reason);
        }

        public static void SendChat(params string[] messages)
        {
            BLTModule.TwitchService.SendChat(messages);
        }
        
        public static void SendReply(string replyId, params string[] messages)
        {
            BLTModule.TwitchService.SendReply(replyId, messages);
        }
    }
}