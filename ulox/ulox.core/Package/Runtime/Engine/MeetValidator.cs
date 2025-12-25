using System.Runtime.CompilerServices;
using System.Linq;

//TODO: these are just used by the vm, move them there

namespace ULox
{
    public static class MeetValidator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool meets, string msg) ValidateClassMeetsClass(UserTypeInternal lhs, UserTypeInternal contract)
        {
            foreach (var contractMeth in contract.Methods)
            {
                if (!lhs.Methods.Get(contractMeth.Key, out var ourContractMatchingMeth))
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
        public static (bool meets, string msg) ValidateInstanceMeetsClass(InstanceInternal lhs, UserTypeInternal contract)
        {
            foreach (var contractMeth in contract.Methods)
            {
                Value ourContractMatchingMeth = Value.Null();
                if (lhs.Fields.Get(contractMeth.Key, out ourContractMatchingMeth)
                    || lhs.FromUserType.Methods.Get(contractMeth.Key, out ourContractMatchingMeth))
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
                if (!lhs.Fields.Get(contractVar, out var _))
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

            if ((lhs.FromUserType == null || lhs.FromUserType is DynamicClass)
                && (contract.FromUserType == null || contract.FromUserType is DynamicClass))
            {
                return InstanceContentMatcher(lhs, contract);
            }

            return ValidateInstanceMeetsClass(lhs, contract.FromUserType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (bool meets, string msg) InstanceContentMatcher(InstanceInternal lhs, InstanceInternal contract)
        {
            foreach (var field in contract.Fields)
            {
                Value ourMatch = Value.Null();
                if (lhs.Fields.Get(field.Key, out ourMatch)
                    || lhs.FromUserType.Methods.Get(field.Key, out ourMatch))
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
            var contractMethArity = contractChunk.ArgumentConstantIds.Count;
            var ourArity = lhsMatchingChunk.ArgumentConstantIds.Count;
            if (ourArity != contractMethArity)
                return (false, $"Expected arity '{contractMethArity}' but found '{ourArity}'.");

            if (lhsMatchingChunk.ReturnConstantIds.Count != contractChunk.ReturnConstantIds.Count)
                return (false, $"Expected return count '{contractChunk.ReturnConstantIds.Count}' but found '{lhsMatchingChunk.ReturnConstantIds.Count}'.");

            return (true, string.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool meets, string msg) ValidateClassMeetsClass(TypeInfoEntry targetType, TypeInfoEntry contractType)
        {
            foreach (var field in contractType.Fields)
            {
                if (!targetType.Fields.Contains(field))
                    return (false, $"Type '{targetType.Name}' does not contain matching field '{field}'.");
            }

            foreach (var method in contractType.Methods)
            {
                var found = targetType.Methods.FirstOrDefault(x => x.ChunkName == method.ChunkName);
                if (found == null)
                    return (false, $"Type '{targetType.Name}' does not contain matching method '{method.ChunkName}'.");

                var (meets, msg) = ChunkMatcher(method, found);
                if(!meets)
                    return (false, msg);
            }

            return (true, string.Empty);
        }
    }
}
