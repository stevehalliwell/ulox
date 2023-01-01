using System.Runtime.CompilerServices;

namespace ULox
{
    public static class ULoxLibraryExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Table GenerateBindingTable(
            this IULoxLibrary self,
            params (string name, Value val)[] bind)
        {
            var resTable = new Table();
            foreach (var (name, val) in bind)
            {
                resTable.Add(new HashedString(name), val);
            }
            return resTable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddFieldsToInstance(
            this InstanceInternal self,
            params (string name, Value val)[] bind)
        {
            foreach (var (name, val) in bind)
            {
                self.SetField(new HashedString(name), val);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddMethodsToClass(
            this UserTypeInternal self,
            params (string name, Value val)[] bind)
        {
            foreach (var (name, val) in bind)
            {
                self.AddMethod(new HashedString(name), val, null);
            }
        }
    }
}
