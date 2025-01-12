using System;
using System.IO;

namespace ULox
{
    public sealed class LocalFileScriptLocator : IScriptLocator
    {
        private readonly DirectoryLimitedFileLocator _locator;

        public LocalFileScriptLocator()
        {
            _locator = new DirectoryLimitedFileLocator(new DirectoryInfo(Environment.CurrentDirectory));
        }

        public Script Find(string name)
        {
            return _locator.Find(name);
        }
    }

    public sealed class DirectoryLimitedFileLocator : IScriptLocator
    {
        private readonly DirectoryInfo _directory;

        public DirectoryInfo DirectoryInfo => _directory;

        public DirectoryLimitedFileLocator(DirectoryInfo dir)
        {
            _directory = dir;
        }

        public Script Find(string name)
        {
            var externalMatches = Directory.GetFiles(_directory.FullName, $"{name}*");
            if (externalMatches?.Length > 0)
                return new Script(name, File.ReadAllText(externalMatches[0]));

            return new Script(name, string.Empty);
        }
    }
}
