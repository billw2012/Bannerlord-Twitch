using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using TaleWorlds.Core;

namespace BannerlordTwitch.Testing
{
    internal class SimulationTest
    {
        private int userId = 0;
        
        private class User
        {
            public string name;
            public DateTime joinTime;
        }

        private readonly List<User> users = new();
        private readonly CancellationTokenSource css;
        private readonly Task updateTask;

        public SimulationTest(Settings settings)
        {
            var simSettings = settings.SimTesting;
            if (simSettings == null)
            {
                Log.ScreenCritical($"Can't run sim, settings need to be specified in the config");
                return;
            }
            
            css = new CancellationTokenSource();
            updateTask = Task.Factory.StartNew(() =>
            {
                var rnd = new Random();
                while (!css.IsCancellationRequested)
                {
                    users.RemoveAll(u => u.joinTime > DateTime.Now + TimeSpan.FromSeconds(simSettings.UserStayTime));
                    while (users.Count < simSettings.UserCount)
                    {
                        var newUser = new User {name = $"user{++userId}", joinTime = DateTime.Now};
                        users.Add(newUser);
                        foreach (var initItem in simSettings.Init)
                        {
                            RunItem(settings, initItem, newUser);
                            Task.Delay(TimeSpan.FromMilliseconds(Math.Max(500, rnd.Next(simSettings.IntervalMinMS, simSettings.IntervalMaxMS))), css.Token).Wait();
                        }
                    }
                    var user = users.SelectRandom();
                    if (user != null)
                    {
                        MainThreadSync.Run(() =>
                        {
                            var item = simSettings.Use.SelectRandom();
                            RunItem(settings, item, user);
                        });
                    }
                    Task.Delay(TimeSpan.FromMilliseconds(Math.Max(500, rnd.Next(simSettings.IntervalMinMS, simSettings.IntervalMaxMS))), css.Token).Wait();
                }
            }, TaskCreationOptions.LongRunning);
        }

        private static void RunItem(Settings settings, SimTestingItem item, User user)
        {
            if (item.Type == "Reward")
            {
                BLTModule.TwitchService.TestRedeem(item.Id, user.name, item.Args);
            }
            else
            {
                var cmd = settings.Commands.FirstOrDefault(c => c.Cmd == item.Id);
                if (cmd != null)
                {
                    RewardManager.Command(cmd.Handler, item.Args, new CommandMessage {}, cmd.HandlerConfig);
                }
            }
        }

        ~SimulationTest()
        {
            Stop();
        }

        public void Stop()
        {
            css.Cancel();
            try
            {
                updateTask.Wait(1000);
            }
            catch
            {
                // ignored
            }
        }
    }
}