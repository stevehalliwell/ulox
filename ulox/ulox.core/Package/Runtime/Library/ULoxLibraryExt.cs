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
            foreach (var item in bind)
            {
                resTable.Add(new HashedString(item.name), item.val);
            }
            return resTable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddFieldsToInstance(
            this InstanceInternal self,
            params (string name, Value val)[] bind)
        {
            foreach (var item in bind)
            {
                self.SetField(new HashedString(item.name), item.val);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddMethodsToClass(
            this UserTypeInternal self,
            params (string name, Value val)[] bind)
        {
            foreach (var item in bind)
            {
                self.AddMethod(new HashedString(item.name), item.val, null);
            }
        }
    }
}
