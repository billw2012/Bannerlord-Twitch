using System;
using System.IO;
using System.Reflection;
using System.Threading;
using TaleWorlds.MountAndBlade;

namespace BLTConfigure
{
    public class BLTConfigureModule : MBSubModuleBase
    {
        public const string Name = "BLTConfigure";
        public const string Ver = "1.0.0";

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
                wnd = new BLTConfigureWindow();
                wnd.Show();
                System.Windows.Threading.Dispatcher.Run();
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
        }
    }
}