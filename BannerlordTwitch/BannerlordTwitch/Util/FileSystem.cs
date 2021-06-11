using System.IO;
using Path = System.IO.Path;
using TaleWorlds.Library;

namespace BannerlordTwitch.Util
{
#if e159
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
#if e159
        public static PlatformFilePath GetConfigPath(string fileName) => new PlatformFilePath(Path.Combine(Common.PlatformFileHelper.DocumentsPath, "Mount and Blade II Bannerlord", "Configs", fileName));
        
        public static bool FileExists(PlatformFilePath path) => File.Exists(path.FilePath);
        public static void SaveFileString(PlatformFilePath path, string str) => File.WriteAllText(path.FilePath, str);
        public static string GetFileContentString(PlatformFilePath path) => File.ReadAllText(path.FilePath);
#else
        public static PlatformFilePath GetConfigPath(string fileName) => new (EngineFilePaths.ConfigsPath, fileName);
        public static bool FileExists(PlatformFilePath path) => Common.PlatformFileHelper.FileExists(path);
        public static void SaveFileString(PlatformFilePath path, string str) => Common.PlatformFileHelper.SaveFileString(path, str);
        public static string GetFileContentString(PlatformFilePath path) => Common.PlatformFileHelper.GetFileContentString(path);
#endif
    }
}