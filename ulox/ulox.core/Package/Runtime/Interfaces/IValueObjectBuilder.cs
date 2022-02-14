namespace ULox
{
    public interface IValueObjectBuilder
    {
        IValueObjectBuilder CreateChild(string name);
        IValueObjectBuilder CreateArray(string name);
        Value Finish();
        void SetField(string name, Value data);
    }
}