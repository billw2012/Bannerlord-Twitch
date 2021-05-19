using System.IO;
using Path = System.IO.Path;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace BannerlordTwitch.Util
{
#if BL_V_1_5_9
    public struct PlatformFilePath
    {
        public PlatformFilePath(string filePath)
        {
            FilePath = filePath;
        }
        public string FilePath;
    }
#endif
    
    public static class FileSystem
    {
#if BL_V_1_5_9
        public static PlatformFilePath GetConfigPath(string fileName) => new PlatformFilePath(Path.Combine(Common.PlatformFileHelper.DocumentsPath, "Mount and Blade II Bannerlord", "Configs", fileName));
        
        public static bool FileExists(PlatformFilePath path) => File.Exists(path.FilePath);
        public static void SaveFileString(PlatformFilePath path, string str) => File.WriteAllText(path.FilePath, str);
        public static string GetFileContentString(PlatformFilePath path) => File.ReadAllText(path.FilePath);
#else
        public static PlatformFilePath GetConfigPath(string fileName) => new (EngineFilePaths.ConfigsPath, fileName);
        private static bool FileExists(PlatformFilePath path) => Common.PlatformFileHelper.FileExists(path);
        private static void SaveFileString(PlatformFilePath path, string str) => Common.PlatformFileHelper.SaveFileString(path, str);
        private static string GetFileContentString(PlatformFilePath path) => Common.PlatformFileHelper.GetFileContentString(path);
#endif
    }
}