namespace ULox
{
    public interface IEngine
    {
        IContext Context { get; }
        IScriptLocator ScriptLocator { get; }

        void LocateAndQueue(string name);
    }
}
