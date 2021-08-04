namespace ULox
{
    public class StandardClassesLibrary : ILoxByteCodeLibrary
    {
        public Table GetBindings()
        {
            var resTable = new Table();
            resTable.Add("List", Value.New(new ListClass()));
            resTable.Add("Dynamic", Value.New(new DynamicClass()));
            return resTable;
        }
    }
}
