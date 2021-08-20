#if e159
using System.IO;
using Path = System.IO.Path;
#endif
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace BannerlordTwitch.Util
{
#if e159
    public struct PlatformDirectoryPath
    {
        public PlatformDirectoryPath(string path)
        {
            Path = path;
        }
        public string Path;

        public override string ToString() => Path;
    }

    public struct PlatformFilePath
    {
        private readonly PlatformDirectoryPath dir;

        public PlatformFilePath(PlatformDirectoryPath dir, string partialPath)
        {
            this.dir = dir;
            this.partialPath = partialPath;
        }
        
        private readonly string partialPath;
        public string FileName => Path.Combine(dir.Path, partialPath);

        public override string ToString() => FileName;
    }
#endif
    
    public static class FileSystem
    {
#if e159
        public static PlatformDirectoryPath GetConfigDir() 
            => new(Path.Combine(Common.PlatformFileHelper.DocumentsPath, "Mount and Blade II Bannerlord", "Configs"));
        public static PlatformFilePath GetConfigPath(string fileName) => new PlatformFilePath(GetConfigDir(), fileName);
        
        public static bool FileExists(PlatformFilePath path) => File.Exists(path.FileName);
        public static void SaveFileString(PlatformFilePath path, string str) => File.WriteAllText(path.FileName, str);
        public static string GetFileContentString(PlatformFilePath path) => File.ReadAllText(path.FileName);
        public static PlatformFilePath[] GetFiles(PlatformDirectoryPath path, string searchPattern) 
            => Directory.GetFiles(path.Path, searchPattern).Select(p => new PlatformFilePath(path, Path.GetFileName(p))).ToArray();
        public static void DeleteFile(PlatformFilePath path) => File.Delete(path.FileName);
#else
        public static PlatformDirectoryPath GetConfigDir() => EngineFilePaths.ConfigsPath;
        public static PlatformFilePath GetConfigPath(string fileName) 
            => new (GetConfigDir(), fileName);
        public static bool FileExists(PlatformFilePath path) 
            => Common.PlatformFileHelper.FileExists(path);
        public static void SaveFileString(PlatformFilePath path, string str) 
            => Common.PlatformFileHelper.SaveFileString(path, str);
        public static string GetFileContentString(PlatformFilePath path) 
            => Common.PlatformFileHelper.GetFileContentString(path);
        public static PlatformFilePath[] GetFiles(PlatformDirectoryPath path, string searchPattern) 
            => Common.PlatformFileHelper.GetFiles(path, searchPattern);
        public static void DeleteFile(PlatformFilePath path) => Common.PlatformFileHelper.DeleteFile(path);
#endif
    }
}