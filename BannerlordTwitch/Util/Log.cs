
using System.Diagnostics;
using TaleWorlds.Core;

namespace BannerlordTwitch.Util
{
    public static class Log
    {
        public static void Info(string str)
        {
            Debug.Print(str);
            #if DEBUG
            InformationManager.DisplayMessage(new InformationMessage(str));
            #endif
        }
    }
}