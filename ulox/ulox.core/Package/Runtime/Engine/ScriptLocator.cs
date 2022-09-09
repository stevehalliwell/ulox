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

        public Script Find(string name)
        {
            var externalMatches = Directory.GetFiles(_directory.FullName, $"{name}*");
            if (externalMatches != null && externalMatches.Length > 0)
                return new Script(name, File.ReadAllText(externalMatches[0]));
            
            return new Script(name,String.Empty);
        }
    }
}
