using System.Collections.Generic;

namespace ULox
{
    public class NativeListInstance : InstanceInternal
    {
        public NativeListInstance()
            : base(NativeListClass.Instance)
        {
        }

        public List<Value> List { get; private set; } = new List<Value>();
    }
}
