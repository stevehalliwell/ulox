using System;
using System.IO;

namespace ULox
{
    public interface IPlatform
    {
        string[] FindFiles(string inDirectory, string withPattern, bool recurse);
    }

    public sealed class Platform : IPlatform
    {
        public string[] FindFiles(string inDirectory, string withPattern, bool recurse)
        {
            var searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var ret = Array.Empty<string>();

            try
            {
                ret = Directory.GetFiles(inDirectory, withPattern, searchOption);
            }
            catch (Exception) { }

            return ret;
        }
    }
}
