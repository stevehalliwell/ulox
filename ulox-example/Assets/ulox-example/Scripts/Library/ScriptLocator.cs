using System.Collections.Generic;

namespace ULox
{
    public class ScriptLocator : IScriptLocator
    {
        private readonly Dictionary<string, string> _builtinScripts = new Dictionary<string, string>();
#if !UNITY_WEBGL
        private readonly DirectoryInfo _directory;
#endif
        public ScriptLocator(
            Dictionary<string, string> builtinScripts,
            string directory)
        {
            _builtinScripts = builtinScripts;
#if !UNITY_WEBGL 
            _directory = new DirectoryInfo(directory);
#endif
        }

        public ScriptLocator()
            : this(new Dictionary<string, string>(),
#if !UNITY_WEBGL 
                  Environment.CurrentDirectory
#else
                  ""
#endif
                  )
        {
        }

        public void Add(string name, string content)
            => _builtinScripts[name] = content;

        public string Find(string name)
        {
            if (_builtinScripts.TryGetValue(name, out var val))
                return val;

#if !UNITY_WEBGL 
            var externalMatches = Directory.GetFiles(_directory.FullName, $"{name}*");
            if (externalMatches != null && externalMatches.Length > 0)
                return File.ReadAllText(externalMatches[0]);
#endif
            return null;
        }
    }
}

//TODO: provide a function to be called when a collision or trigger occures
