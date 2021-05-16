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
    public static partial class ActionManager
    {
        private static readonly Dictionary<string, IRewardHandler> rewardHandlers = new();
        private static readonly Dictionary<string, ICommandHandler> commandHandlers = new();

        public static IEnumerable<string> RewardHandlerNames => rewardHandlers.Keys;
        public static IEnumerable<IRewardHandler> RewardHandlers => rewardHandlers.Values;
        public static IEnumerable<string> CommandHandlerNames => commandHandlers.Keys;
        public static IEnumerable<ICommandHandler> CommandHandlers => commandHandlers.Values;

        public static void Init()
        {
            RegisterAll(typeof(ActionManager).Assembly);
        }

        public static void RegisterAll(Assembly assembly)
        {
            var rewardHandlerTypes = assembly
                .GetTypes()
                .Where(t => typeof(IRewardHandler).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var redemptionActionType in rewardHandlerTypes)
            {
                RegisterRewardHandler((IRewardHandler) Activator.CreateInstance(redemptionActionType));
            }

            var commandHandlerTypes = assembly
                .GetTypes()
                .Where(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var botCommandType in commandHandlerTypes)
            {
                RegisterCommandHandler((ICommandHandler) Activator.CreateInstance(botCommandType));
            }
        }

        public static bool RegisterRewardHandler(IRewardHandler action)
        {
            string id = action.GetType().Name;
            if (rewardHandlers.ContainsKey(id))
            {
                Log.Error($"Reward Handler {id} already registered, please choose another name");
                return false;
            }
            Log.Trace($"Registered Reward Handler {id}");
            rewardHandlers.Add(id, action);
            return true;
        }
        
        public static bool RegisterCommandHandler(ICommandHandler commandHandler)
        {
            string id = commandHandler.GetType().Name;
            if (commandHandlers.ContainsKey(id))
            {
                Log.Error($"Command Handler {id} already registered, please choose another name");
                return false;
            }
            Log.Trace($"Registered Command Handler {id}");
            commandHandlers.Add(id, commandHandler);
            return true;
        }

        public static object FindGlobalConfig(string id) => BLTModule.TwitchService.FindGlobalConfig(id);

        public static void ConvertSettings(IEnumerable<Reward> rewards)
        {
            foreach (var rewardDef in rewards.Where(r => r.HandlerConfig != null))
            {
                if (rewardHandlers.TryGetValue(rewardDef.Handler, out var action))
                {
                    try
                    {
                        rewardDef.HandlerConfig = ConvertObject(rewardDef.HandlerConfig, action.RewardConfigType);
                    }
                    catch (Exception)
                    {
                        Log.Error($"{rewardDef} had invalid config, resetting it to default");
                        rewardDef.HandlerConfig = Activator.CreateInstance(action.RewardConfigType);
                    }
                }
            }
        }
        
        public static void ConvertSettings(IEnumerable<Command> commands)
        {
            foreach (var commandDef in commands.Where(c => c.HandlerConfig != null))
            {
                if (commandHandlers.TryGetValue(commandDef.Handler, out var command))
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

        internal static void HandleCommand(string commandId, ReplyContext context, object config)
        {
            if (commandHandlers.TryGetValue(commandId, out var cmdHandler))
            {
                try
                {
                    if (cmdHandler.HandlerConfigType != null)
                    {
                        config = ConvertObject(config, cmdHandler.HandlerConfigType);
                    }
                    Log.Trace($"[{nameof(ActionManager)}] HandleCommand {commandId} {context.Args} for {context.UserName}");
                    cmdHandler.Execute(context, config);
                }
                catch (Exception e)
                {
                    Log.LogFeedCritical($"Command {commandId} failed with exception {e.Message}, game might be unstable now!");
                    Log.Error(e.ToString());
                }
            }
            else
            {
                // Log.ScreenFail($"Command with id {id} couldn't be found, check Commands config");
            }
        }

        internal static bool HandleReward(string rewardId, ReplyContext context, object config)
        {
            if (!rewardHandlers.TryGetValue(rewardId, out var action))
            {
                Log.Error($"Action with the id {rewardId} doesn't exist");
                return false;
            }

            var st = new Stopwatch();
            st.Start();
            try
            {
                if (action.RewardConfigType != null)
                {
                    config = ConvertObject(config, action.RewardConfigType);
                }
                //Log.Trace($"[{nameof(ActionManager)}] HandleReward {action} {context.Args} for {context.UserName}");
                action.Enqueue(context, config);
            }
            catch (Exception e)
            {
                Log.LogFeedCritical($"Reward {rewardId} failed with exception {e.Message}, game might be unstable now!");
                Log.Error(e.ToString());
                NotifyCancelled(context, $"Error occurred while trying to process the redemption");
            }

            if (st.ElapsedMilliseconds > 5)
            {
                Log.Info($"Action {rewardId} took {st.ElapsedMilliseconds}ms to Enqueue, this is too slow!");
            }

            return true;
        }

        public static void NotifyComplete(ReplyContext context, string status = null)
        {
            BLTModule.TwitchService?.RedemptionComplete(context, status);
        }
        
        public static void NotifyCancelled(ReplyContext context, string reason = null)
        {
            BLTModule.TwitchService?.RedemptionCancelled(context, reason);
        }

        // public static void SendChat(params string[] messages)
        // {
        //     BLTModule.TwitchService.SendChat(messages);
        // }
        
        public static void SendReply(ReplyContext context, params string[] messages)
        {
            BLTModule.TwitchService?.SendReply(context, messages);
        }
    }
}