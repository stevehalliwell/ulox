namespace ULox
{
    public interface IScriptLocator
    {
        void Add(string name, string content);
        string Find(string name);
    }
}
