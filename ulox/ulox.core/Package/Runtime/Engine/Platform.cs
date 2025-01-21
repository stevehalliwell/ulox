using System;
using System.IO;
using System.Linq;

namespace ULox
{
    public interface IPlatformFiles
    {
        string[] FindFiles(string inDirectory, string withPattern, bool recurse);
        string LoadFile(string filePath);
        void SaveFile(string filePath, string contents);
    }

    public interface IPlatformIO
    {
        void Print(string message);
    }

    public interface IPlatform : IPlatformFiles, IPlatformIO
    {
    }

    public sealed class GenericPlatform<T, U> : IPlatform
        where T : IPlatformFiles
        where U : IPlatformIO
    {
        public T Files { get; private set; }
        public U IO { get; private set; }

        public GenericPlatform(T files, U io)
        {
            Files = files;
            IO = io;
        }

        public string[] FindFiles(string inDirectory, string withPattern, bool recurse) => Files.FindFiles(inDirectory, withPattern, recurse);
        public string LoadFile(string filePath) => Files.LoadFile(filePath);
        public void SaveFile(string filePath, string contents) => Files.SaveFile(filePath, contents);
        public void Print(string message) => IO.Print(message);
    }

    public sealed class ConsolePrintPlatform : IPlatformIO
    {
        public void Print(string message) => Console.WriteLine(message);
    }

    public sealed class LogIOPlatform : IPlatformIO
    {
        public LogIOPlatform(Action<string> log) { _logAction = log; }
        private Action<string> _logAction;
        public void Print(string message) => _logAction(message);
    }

    public sealed class DirectoryLimitedPlatform : IPlatformFiles
    {
        private readonly DirectoryInfo _defaultDirectory;
        private readonly (string prefix, DirectoryInfo dir)[] _additionalDirLookUp;

        public DirectoryInfo DirectoryInfo => _defaultDirectory;

        public DirectoryLimitedPlatform(
            DirectoryInfo dir,
            params (string prefix, DirectoryInfo dir)[] additionalDirectories)
        {
            _defaultDirectory = dir;

            _additionalDirLookUp = additionalDirectories;
        }

        public string[] FindFiles(string inDirectory, string withPattern, bool recurse)
        {
            var searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var ret = Array.Empty<string>();

            inDirectory = MakeSafeDir(inDirectory);

            try
            {
                ret = Directory.GetFiles(inDirectory, withPattern, searchOption);
            }
            catch (Exception) { }

            return ret;
        }

        public string LoadFile(string filePath)
        {
            string safePath = MakeFilePathSafe(filePath);
            if (!File.Exists(safePath)) return string.Empty;
            return File.ReadAllText(safePath);
        }

        public void SaveFile(string filePath, string contents)
        {
            string safePath = MakeFilePathSafe(filePath);
            Directory.CreateDirectory(Path.GetDirectoryName(safePath));
            File.WriteAllText(safePath, contents);
        }

        private string MakeFilePathSafe(string filePath)
        {
            var origFile = new FileInfo(MakeRooted(filePath));
            var safeDir = MakeSafeDir(origFile.Directory.FullName);
            var safePath = Path.Combine(safeDir, origFile.Name);
            return safePath;
        }

        private string MakeRooted(string partial)
        {
            //if it starts with a prefix, use that directory
            foreach (var (prefix, dir) in _additionalDirLookUp)
            {
                if (partial.StartsWith(prefix))
                {
                    partial = Path.Combine(dir.FullName, partial.Substring(prefix.Length));
                    break;
                }
            }

            if (!Path.IsPathRooted(partial))
                partial = Path.Combine(_defaultDirectory.FullName, partial);
            return Path.GetFullPath(partial);
        }

        private string MakeSafeDir(string partial)
        {
            var path = MakeRooted(partial);
            var safePath = Path.GetFullPath(path);
            if (!Directory.Exists(safePath)) return _defaultDirectory.FullName;  //enjoy the default
            if (_additionalDirLookUp.Any(x => safePath.StartsWith(x.dir.FullName))) return safePath;  //matching valid folder
            if (safePath.StartsWith(_defaultDirectory.FullName) == false) return _defaultDirectory.FullName;  //enjoy the default
            return safePath;
        }
    }
}
