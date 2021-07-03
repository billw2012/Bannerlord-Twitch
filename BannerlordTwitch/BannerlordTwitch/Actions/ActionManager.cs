using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Bannerlord.ButterLib.Common.Extensions;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using YamlDotNet.Serialization;

namespace BannerlordTwitch.Rewards
{
    public static partial class ActionManager
    {
        private static readonly Dictionary<string, IRewardHandler> rewardHandlers = new();
        private static readonly Dictionary<string, ICommandHandler> commandHandlers = new();
        private static readonly Dictionary<string, Type> globalConfigTypes = new();

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

        public static bool RegisterGlobalConfigType(string key, Type settingsType)
        {
            if (globalConfigTypes.ContainsKey(key))
            {
                Log.Error($"Global Settings with key {key}={settingsType} already registered, please choose another name");
                return false;
            }
            Log.Trace($"Registered Global Settings {key}={settingsType}");
            globalConfigTypes.Add(key, settingsType);
            return true;
        }

        public static T GetGlobalConfig<T>(string id)
        {
            if (!globalConfigTypes.TryGetValue(id, out var type))
            {
                Log.Error($"Global Settings Id {id} type not registered (use ActionManager.RegisterGlobalSettingsType)");
                return default;
            }

            if(type != typeof(T))
            {
                Log.Error($"Registered type {type} of Global Settings {id} doesn't match request type {typeof(T)}");
                return default;
            }
            
            return (T) BLTModule.TwitchService.FindGlobalConfig(id);
        }

        public static void ConvertSettings(IEnumerable<Reward> rewards)
        {
            foreach (var rewardDef in rewards)
            {
                if (rewardHandlers.TryGetValue(rewardDef.Handler, out var action))
                {
                    if (rewardDef.HandlerConfig == null && action.RewardConfigType != null)
                    {
                        rewardDef.HandlerConfig = Activator.CreateInstance(action.RewardConfigType);
                    }
                    else if (rewardDef.HandlerConfig != null && action.RewardConfigType == null)
                    {
                        rewardDef.HandlerConfig = null;
                    }
                    else
                    {
                        try
                        {
                            rewardDef.HandlerConfig = action.RewardConfigType != null
                                ? YamlHelpers.ConvertObject(rewardDef.HandlerConfig, action.RewardConfigType)
                                : null;
                        }
                        catch (Exception)
                        {
                            Log.Error($"{rewardDef} had invalid config, resetting it to default");
                            rewardDef.HandlerConfig = Activator.CreateInstance(action.RewardConfigType);
                        }
                    }
                }
            }
        }
        
        public static void ConvertSettings(IEnumerable<Command> commands)
        {
            foreach (var commandDef in commands)
            {
                if (commandHandlers.TryGetValue(commandDef.Handler, out var command))
                {
                    if (commandDef.HandlerConfig == null && command.HandlerConfigType != null)
                    {
                        commandDef.HandlerConfig = Activator.CreateInstance(command.HandlerConfigType);
                    }
                    else if (commandDef.HandlerConfig != null && command.HandlerConfigType == null)
                    {
                        commandDef.HandlerConfig = null;
                    }
                    else
                    {
                        try
                        {
                            commandDef.HandlerConfig = command.HandlerConfigType != null
                                ? YamlHelpers.ConvertObject(commandDef.HandlerConfig, command.HandlerConfigType)
                                : null;
                        }
                        catch (Exception)
                        {
                            Log.Error($"{commandDef} had invalid config, resetting it to default");
                            commandDef.HandlerConfig = Activator.CreateInstance(command.HandlerConfigType);
                        }
                    }
                }
            }
        }

        public static void EnsureGlobalSettings(ICollection<GlobalConfig> globalSettings)
        {
            // Make sure all expected global configs exist 
            foreach ((string id, var configType) in globalConfigTypes)
            {
                var globalConfig = globalSettings.FirstOrDefault(g => g.Id == id);
                if (globalConfig == null)
                {
                    // Create new default
                    globalSettings.Add(new GlobalConfig { Id = id, Config = Activator.CreateInstance(configType)});
                }
                else
                {
                    try
                    {
                        // Convert from anonymous to typed object
                        globalConfig.Config = YamlHelpers.ConvertObject(globalConfig.Config, configType);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"{globalConfig} had invalid config ({ex.Message}), resetting it to default");
                        globalConfig.Config = Activator.CreateInstance(configType);
                    }
                    
                }
            }

            // Remove old settings
            var toRemove = globalSettings.Where(s => globalConfigTypes.All(t => s.Id != t.Key)).ToList();
            foreach (var r in toRemove)
            {
                globalSettings.Remove(r);
            }
        }

        internal static void HandleCommand(string commandId, ReplyContext context, object config)
        {
            if (Campaign.Current == null)
            {
                SendReply(context, NotStartedMessage);
                return;
            }

            if (commandHandlers.TryGetValue(commandId, out var cmdHandler))
            {
                if (cmdHandler.HandlerConfigType != null)
                {
                    config = YamlHelpers.ConvertObject(config, cmdHandler.HandlerConfigType);
                }
                Log.Trace($"[{nameof(ActionManager)}] HandleCommand {commandId} {context.Args} for {context.UserName}");
                cmdHandler.Execute(context, config);
            }
            else
            {
                // Log.ScreenFail($"Command with id {id} couldn't be found, check Commands config");
            }
        }

        public const string NotStartedMessage = "The game isn't started yet";

        internal static bool HandleReward(string rewardId, ReplyContext context, object config)
        {
            if (Campaign.Current == null)
            {
                NotifyCancelled(context, NotStartedMessage);
                return false;
            }
            
            if (!rewardHandlers.TryGetValue(rewardId, out var action))
            {
                NotifyCancelled(context, $"Action with the id {rewardId} doesn't exist");
                return false;
            }

            var st = new Stopwatch();
            st.Start();

            if (action.RewardConfigType != null)
            {
                config = YamlHelpers.ConvertObject(config, action.RewardConfigType);
            }
            //Log.Trace($"[{nameof(ActionManager)}] HandleReward {action} {context.Args} for {context.UserName}");
            action.Enqueue(context, config);

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

        public static void SendChat(params string[] messages)
        {
            BLTModule.TwitchService?.SendChat(messages);
        }
        
        public static void SendReply(ReplyContext context, params string[] messages)
        {
            BLTModule.TwitchService?.SendReply(context, messages);
        }
    }
}