namespace ULox
{
    public interface IValueHeirarchyWriter
    {
        void StartNamedElement(string name);
        void StartElement();
        void EndElement();
        void StartNamedArray(string name);
        void StartArray();
        void EndArray();
        void WriteNameAndValue(string name, Value v);
        void WriteValue(Value v);
    }
}
