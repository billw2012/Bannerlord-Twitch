using System;
using System.Collections.Generic;
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

        #region Names
        private static string[] Names =
        {
            "Maias", "Wesley-Scott", "Baley", "Desmond", "Bradyn", "Jules", "Kynan", "Ruan", "Artem", "Aryn",
            "Ireoluwa", "Koddi", "Johnathan", "Cailean", "Paolo", "Abubakar", "Presley", "McKay", "Peiyan", "Lenin",
            "Khaleel", "Chester", "Luqman", "Kaelin", "Fergal", "Vladimir", "Blazey", "Harper", "Sher", "Leydon",
            "Aiadan", "Carson", "Aldred", "Ace", "Sukhvir", "Ze", "Sylvain", "Conghaile", "Arya", "Connel", "Kaison",
            "Abdisalam", "Samy", "Diesel", "Muhammed", "Yuanyu", "Aaron-James", "Uzayr", "Kurtis", "Eroni",
        };
        #endregion
        
        private class User
        {
            public string name;
            public DateTime leaveTime;
        }

        private readonly List<User> users = new();
        private readonly CancellationTokenSource css;
        private readonly Task updateTask;

        public SimulationTest(Settings settings)
        {
            var simSettings = settings.SimTesting;
            if (simSettings == null)
            {
                Log.LogFeedCritical($"Can't run sim, settings need to be specified in the config");
                return;
            }

            //string[] randomNames = Names.Shuffle().ToArray();
            css = new CancellationTokenSource();
            updateTask = Task.Factory.StartNew(() =>
            {
                var rnd = new Random();
                while (!css.IsCancellationRequested)
                {
                    users.RemoveAll(u => DateTime.Now > u.leaveTime);
                    while (users.Count < simSettings.UserCount)
                    {
                        string name = Names[userId % Names.Length];
                        if (rnd.NextDouble() < 0.5f) name = name.ToLower();
                        
                        var newUser = new User
                        {
                            name = $"{name}{++userId}",
                            leaveTime = DateTime.Now +
                                        TimeSpan.FromSeconds(rnd.Next((int) (simSettings.UserStayTime * 0.75f),
                                            (int) (simSettings.UserStayTime * 1.25f)))
                        };
                        users.Add(newUser);
                        foreach (var initItem in simSettings.Init)
                        {
                            RunItem(settings, initItem, newUser);
                            Task.Delay(TimeSpan.FromMilliseconds(Math.Max(100, rnd.Next(simSettings.IntervalMinMS, simSettings.IntervalMaxMS))), css.Token).Wait();
                        }
                    }
                    var user = users.SelectRandom();
                    if (user != null && simSettings.Use != null)
                    {
                        MainThreadSync.Run(() =>
                        {
                            var item = simSettings.Use.SelectWeighted((float) rnd.NextDouble(), testingItem => testingItem.Weight <= 0? 1 : testingItem.Weight);
                            RunItem(settings, item, user);
                        });
                    }
                    Task.Delay(TimeSpan.FromMilliseconds(Math.Max(100, rnd.Next(simSettings.IntervalMinMS, simSettings.IntervalMaxMS))), css.Token).Wait();
                }
            }, TaskCreationOptions.LongRunning);
        }

        private static void RunItem(Settings settings, SimTestingItem item, User user)
        {
            if (item.Type == "Reward")
            {
                BLTModule.TwitchService?.TestRedeem(item.Id, user.name, item.Args);
            }
            else
            {
                var cmd = settings.EnabledCommands.FirstOrDefault(c => c.Name == item.Id);
                if (cmd != null)
                {
                    ActionManager.HandleCommand(cmd.Handler, ReplyContext.FromUser(cmd, user.name), cmd.HandlerConfig);
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