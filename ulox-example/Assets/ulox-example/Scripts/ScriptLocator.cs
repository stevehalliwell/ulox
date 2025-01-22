using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ULox
{
    public class ScriptLocator : IPlatform
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
            if (_builtinScripts != null
                && _builtinScripts.TryGetValue(name, out var val))
                return new Script(name, val);

#if !UNITY_WEBGL 
            var nameSearch = $"{name}*";
            var externalMatches = System.IO.Directory.GetFiles(_directory.FullName, nameSearch);
            if (externalMatches == null
                || externalMatches.Length == 0)
                throw new System.IO.FileNotFoundException(nameSearch);

            return new Script(name, System.IO.File.ReadAllText(externalMatches[0]));
#endif
            return new Script(name, null);
        }

        public string[] FindFiles(string inDirectory, string withPattern, bool recurse)
        {
            var searchOption = recurse ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;
            var ret = new string[0];

            try
            {
                var fullPath = System.IO.Path.Combine(_directory.FullName, inDirectory);
                if (fullPath.StartsWith(_directory.FullName))
                {
                    var prefixLen = _directory.FullName.Length + 1;
                    ret = System.IO.Directory.GetFiles(fullPath, withPattern, searchOption);
                    ret = ret.Select(x => x.Substring(prefixLen)).ToArray();
                }
            }
            catch (Exception) { }

            return ret;
        }

        public string LoadFile(string filePath)
        {
            throw new NotImplementedException();
        }

        public void SaveFile(string filePath, string contents)
        {
            throw new NotImplementedException();
        }

        public void Print(string message)
        {
            Debug.Log(message);
        }
    }
}