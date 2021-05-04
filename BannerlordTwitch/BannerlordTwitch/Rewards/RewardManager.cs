using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BannerlordTwitch.Util;
using TaleWorlds.Library;
using YamlDotNet.Serialization;
using Color = System.Windows.Media.Color;

namespace BannerlordTwitch.Rewards
{
    public static partial class RewardManager
    {
        private static readonly Dictionary<string, IAction> actions = new();
        private static readonly Dictionary<string, ICommandHandler> commands = new();

        public static IEnumerable<string> ActionNames => actions.Keys;
        public static IEnumerable<IAction> Actions => actions.Values;
        public static IEnumerable<string> HandlerNames => commands.Keys;
        public static IEnumerable<ICommandHandler> Handlers => commands.Values;

        public static void Init()
        {
            RegisterAll(typeof(RewardManager).Assembly);
        }

        public static void RegisterAll(Assembly assembly)
        {
            var redemptionActionTypes = assembly
                .GetTypes()
                .Where(t => typeof(IAction).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var redemptionActionType in redemptionActionTypes)
            {
                RegisterAction((IAction) Activator.CreateInstance(redemptionActionType));
            }

            var botCommands = assembly
                .GetTypes()
                .Where(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var botCommandType in botCommands)
            {
                RegisterCommand((ICommandHandler) Activator.CreateInstance(botCommandType));
            }
        }

        public static bool RegisterAction(IAction action)
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
        
        public static bool RegisterCommand(ICommandHandler command)
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

        public static object FindGlobalConfig(string id) => BLTModule.TwitchService.FindGlobalConfig(id);

        public static void ConvertSettings(IEnumerable<Reward> rewards)
        {
            foreach (var rewardDef in rewards.Where(r => r.ActionConfig != null))
            {
                if (actions.TryGetValue(rewardDef.Action, out var action))
                {
                    try
                    {
                        rewardDef.ActionConfig = ConvertObject(rewardDef.ActionConfig, action.ActionConfigType);
                    }
                    catch (Exception)
                    {
                        Log.Error($"{rewardDef} had invalid config, resetting it to default");
                        rewardDef.ActionConfig = Activator.CreateInstance(action.ActionConfigType);
                    }
                }
            }
        }
        
        public static void ConvertSettings(IEnumerable<Command> commands)
        {
            foreach (var commandDef in commands.Where(c => c.HandlerConfig != null))
            {
                if (RewardManager.commands.TryGetValue(commandDef.Handler, out var command))
                {
                    try
                    {
                        commandDef.HandlerConfig = ConvertObject(commandDef.HandlerConfig, command.HandlerConfigType);
                    }
                    catch (Exception)
                    {
                        Log.Error($"{commandDef} had invalid config, resetting it to default");
                        commandDef.HandlerConfig = Activator.CreateInstance(command.HandlerConfigType);
                    }
                }
            }
        }
        
        internal static object ConvertObject(object obj, Type type) =>
            new DeserializerBuilder().Build().Deserialize(
                new SerializerBuilder().Build().Serialize(obj),
                type);

        internal static void Command(string id, ReplyContext context, object config)
        {
            if (commands.TryGetValue(id, out var cmdHandler))
            {
                try
                {
                    if (cmdHandler.HandlerConfigType != null)
                    {
                        config = ConvertObject(config, cmdHandler.HandlerConfigType);
                    }

                    cmdHandler.Execute(context, config);
                }
                catch (Exception e)
                {
                    Log.LogFeedCritical($"Command {id} failed with exception {e.Message}, game might be unstable now!");
                    Log.Error(e.ToString());
                }
            }
            else
            {
                // Log.ScreenFail($"Command with id {id} couldn't be found, check Commands config");
            }
        }

        internal static bool Enqueue(string actionId, ReplyContext context, object config)
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
                if (action.ActionConfigType != null)
                {
                    config = ConvertObject(config, action.ActionConfigType);
                }
                
                action.Enqueue(context, config);
            }
            catch (Exception e)
            {
                Log.LogFeedCritical($"Action {actionId} failed with exception {e.Message}, game might be unstable now!");
                Log.Error(e.ToString());
                NotifyCancelled(context, $"Error occurred while trying to process the redemption");
            }

            if (st.ElapsedMilliseconds > 5)
            {
                Log.Info($"Action {actionId} took {st.ElapsedMilliseconds}ms to Enqueue, this is too slow!");
            }

            return true;
        }

        public static void NotifyComplete(ReplyContext context, string status = null)
        {
            BLTModule.TwitchService.RedemptionComplete(context, status);
        }
        
        public static void NotifyCancelled(ReplyContext context, string reason = null)
        {
            BLTModule.TwitchService.RedemptionCancelled(context, reason);
        }

        // public static void SendChat(params string[] messages)
        // {
        //     BLTModule.TwitchService.SendChat(messages);
        // }
        
        public static void SendReply(ReplyContext context, params string[] messages)
        {
            BLTModule.TwitchService.SendReply(context, messages);
        }
    }
}