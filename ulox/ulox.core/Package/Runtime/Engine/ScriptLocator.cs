using System;
using System.IO;

namespace ULox
{
    public class LocalFileScriptLocator : IScriptLocator
    {
        private readonly DirectoryInfo _directory;
        
        public LocalFileScriptLocator()
        {
            _directory = new DirectoryInfo(Environment.CurrentDirectory);
        }

        public string Find(string name)
        {
            var externalMatches = Directory.GetFiles(_directory.FullName, $"{name}*");
            if (externalMatches != null && externalMatches.Length > 0)
                return File.ReadAllText(externalMatches[0]);
            
            return null;
        }
    }
}
