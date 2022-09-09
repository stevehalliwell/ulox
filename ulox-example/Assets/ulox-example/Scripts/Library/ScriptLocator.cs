using System.Collections.Generic;

namespace ULox
{
    public class ScriptLocator : IScriptLocator
    {
        private readonly Dictionary<string, string> _builtinScripts = new Dictionary<string, string>();
#if !UNITY_WEBGL
        private readonly System.IO.DirectoryInfo _directory;
#endif
        public ScriptLocator(
            Dictionary<string, string> builtinScripts,
            string directory)
        {
            _builtinScripts = builtinScripts;
#if !UNITY_WEBGL 
            _directory = new System.IO.DirectoryInfo(directory);
#endif
        }

        public ScriptLocator()
            : this(new Dictionary<string, string>(),
#if !UNITY_WEBGL 
                  System.Environment.CurrentDirectory
#else
                  ""
#endif
                  )
        {
        }

        public void Add(string name, string content)
            => _builtinScripts[name] = content;

        public Script Find(string name)
        {
            if (_builtinScripts.TryGetValue(name, out var val))
                return new Script(name,val);

#if !UNITY_WEBGL 
            var externalMatches = System.IO.Directory.GetFiles(_directory.FullName, $"{name}*");
            if (externalMatches != null && externalMatches.Length > 0)
                return new Script(name,System.IO.File.ReadAllText(externalMatches[0]));
#endif
            return new Script(name, null);
        }
    }
}

//TODO: provide a function to be called when a collision or trigger occures
