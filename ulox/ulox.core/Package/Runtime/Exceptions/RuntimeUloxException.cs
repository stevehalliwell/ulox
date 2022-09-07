namespace ULox
{
    public class RuntimeUloxException : UloxException
    {
        public RuntimeUloxException(string msg, int currentInstruction, string locationName)
            : base($"{msg} at ip:'{currentInstruction}' in chunk:'{locationName}'.")
        {
        }
    }
}
