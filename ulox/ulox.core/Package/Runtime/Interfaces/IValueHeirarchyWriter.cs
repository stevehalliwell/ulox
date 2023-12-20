namespace ULox
{
    public interface IValueHeirarchyWriter
    {
        void StartNamedElement(string name);
        void StartElement();
        void EndElement();
        void StartArray(string name);
        void EndArray();
        void WriteNameAndValue(string name, Value v);
        void WriteValue(Value v);
    }
}
