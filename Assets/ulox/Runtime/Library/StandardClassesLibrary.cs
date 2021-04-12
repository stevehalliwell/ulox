namespace ULox
{
    public class StandardClassesLibrary : ILoxByteCodeLibrary
    {
        public void BindToEngine(ByteCodeInterpreterEngine engine)
        {
            engine.VM.SetGlobal("List", Value.New(new ListClass()));
        }
    }
}
