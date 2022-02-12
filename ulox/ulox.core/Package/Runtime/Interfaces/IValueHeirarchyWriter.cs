namespace ULox
{
    public interface IValueHeirarchyWriter
    {
        void WriteNameAndValue(string name, Value v);

        void StartElement(string name, Value v);

        void EndElement(string name, Value v);
    }
}
