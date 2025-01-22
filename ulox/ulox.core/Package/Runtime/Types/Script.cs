namespace ULox
{
    public class Script
    {
        public readonly string Name;
        public readonly string Source;
        public readonly int ScriptHash;

        public Script(string name, string source)
        {
            Name = name;
            Source = source;
            ScriptHash = source.GetHashCode();  //todo no good, use a stable one
        }
    }
}