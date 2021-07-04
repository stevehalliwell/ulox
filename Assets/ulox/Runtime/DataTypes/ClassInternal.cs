using System.Collections.Generic;

namespace ULox
{
    public class ClassInternal : InstanceInternal
    {
        public string name;
        public Dictionary<string, Value> methods = new Dictionary<string, Value>();
        public List<string> properties = new List<string>();
        public Value initialiser = Value.Null();
        public ClosureInternal initChainStartClosure;
        public int initChainStartLocation = -1;
    }
}
