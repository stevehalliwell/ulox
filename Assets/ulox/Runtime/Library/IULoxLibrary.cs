namespace ULox
{
    public interface IULoxLibrary
    {
        public string Name { get; }
        Table GetBindings();
    }

    public static class ULoxLibraryExt
    {
        public static Table GenerateBindingTable(
            this IULoxLibrary self,
            params (string name, Value val)[] bind)
        {
            var resTable = new Table();
            foreach (var item in bind)
            {
                resTable.Add(new HashedString(item.name), item.val);
            }
            return resTable;
        }

        public static void AddFieldsToInstance(
            this InstanceInternal self,
            params (string name, Value val)[] bind)
        {
            foreach (var item in bind)
            {
                self.SetField(new HashedString(item.name), item.val);
            }
        }

        public static void AddMethodsToClass(
            this ClassInternal self,
            params (string name, Value val)[] bind)
        {
            foreach (var item in bind)
            {
                self.AddMethod(new HashedString(item.name), item.val);
            }
        }
    }
}
