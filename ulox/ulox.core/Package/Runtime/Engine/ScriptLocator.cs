using System.Collections.Generic;
using System.IO;

namespace ULox
{
    public class ScriptLocator : IScriptLocator
    {
        private readonly Dictionary<string, string> _builtinScripts;
        private readonly DirectoryInfo _directory;

        public ScriptLocator(
            Dictionary<string, string> builtinScripts,
            DirectoryInfo directory)
        {
            _builtinScripts = builtinScripts;
            _directory = directory;
        }

        public ScriptLocator()
        {
            _builtinScripts = new Dictionary<string, string>();
            _directory = new DirectoryInfo(System.Environment.CurrentDirectory);
        }

        public void Add(string name, string content)
        {
            _builtinScripts[name] = content;
        }

        public string Find(string name)
        {
            if (_builtinScripts.TryGetValue(name, out var val))
                return val;

            var externalMatches = Directory.GetFiles(_directory.FullName, $"{name}*");
            if (externalMatches != null && externalMatches.Length > 0)
                return File.ReadAllText(externalMatches[0]);

            return null;
        }
    }
}
