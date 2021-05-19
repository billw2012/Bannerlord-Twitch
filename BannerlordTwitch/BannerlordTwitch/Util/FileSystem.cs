using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace BannerlordTwitch.Util
{
    public static class FileSystem
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

        private static PlatformFilePath GetConfigPath(string fileName) => new PlatformFilePath(Path.Combine(Common.PlatformFileHelper.DocumentsPath, "Mount and Blade II Bannerlord", "Configs", fileName));
        
        private static bool FileExists(PlatformFilePath path) => File.Exists(path.FilePath);
        private static void SaveFileString(PlatformFilePath path, string str) => File.WriteAllText(path.FilePath, str);
        private static string GetFileContentString(PlatformFilePath path) => File.ReadAllText(path.FilePath);
#else
        public static PlatformFilePath GetConfigPath(string fileName) => new (EngineFilePaths.ConfigsPath, fileName);
        private static bool FileExists(PlatformFilePath path) => Common.PlatformFileHelper.FileExists(path);
        private static void SaveFileString(PlatformFilePath path, string str) => Common.PlatformFileHelper.SaveFileString(path, str);
        private static string GetFileContentString(PlatformFilePath path) => Common.PlatformFileHelper.GetFileContentString(path);
#endif
    }
}