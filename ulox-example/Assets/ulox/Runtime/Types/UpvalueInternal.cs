namespace ULox
{
    public class UpvalueInternal
    {
        public int index = -1;
        public bool isClosed = false;
        public Value value = Value.Null();
    }
}
