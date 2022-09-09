using System.Collections.Generic;

namespace ULox
{
    public readonly struct Script
    {
        public readonly string Name;
        public readonly string Source;

        public Script(string name, string source)
        {
            Name = name;
            Source = source;
        }

        public override int GetHashCode()
        {
            int hashCode = -2113663506;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Source);
            return hashCode;
        }
    }
}
