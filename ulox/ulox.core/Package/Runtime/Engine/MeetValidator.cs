using System.Runtime.CompilerServices;
using System.Linq;

namespace ULox
{
    public static class MeetValidator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool meets, string msg) ValidateClassMeetsClass(ClassInternal lhs, ClassInternal contract)
        {
            foreach (var contractMeth in contract.Methods)
            {
                if (!lhs.Methods.TryGetValue(contractMeth.Key, out var ourContractMatchingMeth))
                    return (false, $"'{lhs.Name.String}' does not contain matching method '{contractMeth.Key.String}'.");

                var contractChunk = contractMeth.Value.val.asClosure.chunk;
                var ourContractMatchingChunk = ourContractMatchingMeth.val.asClosure.chunk;
                var res = ChunkMatcher(ourContractMatchingChunk, contractChunk);
                if (!res.meets)
                    return res;

            }

            foreach (var contractVar in contract.FieldNames)
            {
                if (lhs.FieldNames.FirstOrDefault(x => x == contractVar) == null)
                {
                    return (false, $"'{lhs.Name.String}' has no field of name '{contractVar.String}'.");
                }
            }
            return (true, string.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool meets, string msg) ValidateInstanceMeetsClass(InstanceInternal lhs, ClassInternal contract)
        {
            foreach (var contractMeth in contract.Methods)
            {
                Value ourContractMatchingMeth = Value.Null();
                if (lhs.Fields.TryGetValue(contractMeth.Key, out ourContractMatchingMeth)
                    || lhs.FromClass.Methods.TryGetValue(contractMeth.Key, out ourContractMatchingMeth))
                {
                    if (contractMeth.Value.type == ValueType.Closure
                        && ourContractMatchingMeth.type == ValueType.Closure)
                    {
                        var res = ChunkMatcher(ourContractMatchingMeth.val.asClosure.chunk, contractMeth.Value.val.asClosure.chunk);
                        if (!res.meets)
                            return res;
                    }
                }
                else
                {
                    return (false, $"instance does not contain matching method '{contractMeth.Key.String}'.");
                }
            }

            foreach (var contractVar in contract.FieldNames)
            {
                if (!lhs.Fields.TryGetValue(contractVar, out var _))
                {
                    return (false, $"instance does not contain matching field '{contractVar.String}'.");
                }
            }

            return (true, string.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool meets, string msg) ValidateInstanceMeetsInstance(InstanceInternal lhs, InstanceInternal contract)
        {
            if (lhs.GetType() != contract.GetType())
                return (false, $"instance does not match internal type, expected '{contract.GetType()}' but found '{lhs.GetType()}'.");

            if ((lhs.FromClass == null || lhs.FromClass is DynamicClass)
                && (contract.FromClass == null || contract.FromClass is DynamicClass))
            {
                return InstanceContentMatcher(lhs, contract);
            }

            return ValidateInstanceMeetsClass(lhs, contract.FromClass);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (bool meets, string msg) InstanceContentMatcher(InstanceInternal lhs, InstanceInternal contract)
        {
            foreach (var field in contract.Fields.AsReadOnly)
            {
                Value ourMatch = Value.Null();
                if (lhs.Fields.TryGetValue(field.Key, out ourMatch)
                    || lhs.FromClass.Methods.TryGetValue(field.Key, out ourMatch))
                {
                    if (field.Value.type != ourMatch.type)
                        return (false, $"instance has matching field name '{field.Key.String}' but type does not match, expected '{ourMatch.type}' but found '{field.Value.type}'.");

                    switch (field.Value.type)
                    {
                    case ValueType.Closure:
                        var contractMeth = field.Value.val.asClosure.chunk;
                        var resClosure = ChunkMatcher(ourMatch.val.asClosure.chunk, contractMeth);
                        if (!resClosure.meets)
                            return resClosure;
                        break;
                    case ValueType.Instance:
                        var resInst = ValidateInstanceMeetsInstance(ourMatch.val.asInstance, field.Value.val.asInstance);
                        if (!resInst.meets)
                            return resInst;
                        break;
                    default:
                        break;
                    }
                }
                else
                {
                    return (false, $"instance does not contain matching field '{field.Key.String}'.");
                }
            }
            return (true, string.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (bool meets, string msg) ChunkMatcher(Chunk lhsMatchingChunk, Chunk contractChunk)
        {
            var contractMethArity = contractChunk.Arity;
            var ourArity = lhsMatchingChunk.Arity;
            if (ourArity != contractMethArity)
                return (false, $"Expected arity '{contractMethArity}' but found '{ourArity}'.");

            if (contractChunk.IsLocal && !lhsMatchingChunk.IsLocal)
                return (false, $"Expected local but found '{lhsMatchingChunk.FunctionType}'.");

            if (contractChunk.IsPure && !lhsMatchingChunk.IsPure)
                return (false, $"Expected pure but found '{lhsMatchingChunk.FunctionType}'.");

            return (true, string.Empty);
        }
    }
}
