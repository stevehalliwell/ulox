using System.Collections.Generic;
using System.IO;

namespace ULox
{
    public class ScriptLocator: IScriptLocator
    {
        private Dictionary<string, string> _builtinScripts;
        private DirectoryInfo _directory;

        public ScriptLocator(
            Dictionary<string, string> builtinScripts,
            DirectoryInfo directory)
        {
            _builtinScripts = builtinScripts;
            _directory = directory;
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
