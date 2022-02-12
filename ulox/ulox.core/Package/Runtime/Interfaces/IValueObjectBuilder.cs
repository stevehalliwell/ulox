namespace ULox
{
    public interface IValueObjectBuilder
    {
        IValueObjectBuilder CreateChild(string prevNodeName);
        Value Finish();
        void SetField(string name, string data);
    }
}