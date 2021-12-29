namespace ULox
{
    public class Upvalue
    {
        public byte index;
        public bool isLocal;

        public Upvalue(byte index, bool isLocal)
        {
            this.index = index;
            this.isLocal = isLocal;
        }
    }
}
