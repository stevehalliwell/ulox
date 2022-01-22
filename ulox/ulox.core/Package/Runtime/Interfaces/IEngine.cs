namespace ULox
{
    public interface IEngine
    {
        IContext Context { get; }

        void LocateAndQueue(string name);
        void RunScript(string script);
    }
}
