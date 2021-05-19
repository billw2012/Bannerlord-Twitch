using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using TaleWorlds.MountAndBlade;

namespace BLTConfigure
{
    public class BLTConfigureModule : MBSubModuleBase
    {
        public const string Name = "BLTConfigure";
        public const string Ver = "1.2.0";

        private readonly Thread thread;
        private BLTConfigureWindow wnd;

        public BLTConfigureModule()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                string folderPath = Path.GetDirectoryName(typeof(BLTConfigureModule).Assembly.Location);
                string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
                if (!File.Exists(assemblyPath)) return null;
                var assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            };
            
            thread = new Thread(() =>
            {
                try
                {
                    wnd = new BLTConfigureWindow { ShowActivated = false };
                    wnd.Show();
                    System.Windows.Threading.Dispatcher.Run();
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Exception occurred (please report this on the discord or nexusmods):\n{e}", "BLT Configure Module Crashed!");
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        protected override void OnSubModuleUnloaded()
        {
            wnd.Dispatcher.Invoke(() =>
            {
                wnd.Close();
            });
            wnd.Dispatcher.InvokeShutdown();
            thread.Join(TimeSpan.FromMilliseconds(500));
        }
    }
}