using System.Runtime.CompilerServices;
using System.Linq;

namespace ULox
{
    public static class MeetValidator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateClassMeetsClass(ClassInternal lhs, ClassInternal contract)
        {
            foreach (var contractMeth in contract.Methods)
            {
                if (!lhs.Methods.TryGetValue(contractMeth.Key, out var ourContractMatchingMeth))
                    throw new VMException($"Meets violation. '{lhs.Name.String}' meets '{contract.Name.String}' does not contain matching method '{contractMeth.Key.String}'.");

                var contractChunk = contractMeth.Value.val.asClosure.chunk;
                var contractMethArity = contractChunk.Arity;
                var ourContractMatchingChunk = ourContractMatchingMeth.val.asClosure.chunk;
                var ourArity = ourContractMatchingChunk.Arity;
                if (ourArity != contractMethArity)
                    throw new VMException($"Meets violation. '{lhs.Name.String}' meets '{contract.Name.String}' has method '{contractMeth.Key.String}' but expected arity of '{contractMethArity}' but has '{ourArity}'.");

                if (contractChunk.IsLocal && !ourContractMatchingChunk.IsLocal)
                    throw new VMException($"Meets violation. '{lhs.Name.String}' meets '{contract.Name.String}' expected local but is of type '{ourContractMatchingChunk.FunctionType}'.");

                if (contractChunk.IsPure && !ourContractMatchingChunk.IsPure)
                    throw new VMException($"Meets violation. '{lhs.Name.String}' meets '{contract.Name.String}' expected pure but is of type '{ourContractMatchingChunk.FunctionType}'.");
            }

            //todo
            foreach (var contractVar in contract.FieldNames)
            {
                if (lhs.FieldNames.FirstOrDefault(x => x == contractVar) == null)
                {
                    throw new VMException($"Meets violation. '{lhs.Name.String}' meets '{contract.Name.String}' has no field of name '{contractVar.String}'.");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ValidateInstanceMeetsClass(InstanceInternal lhs, ClassInternal contract)
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
                        if (!ChunkMatcher(ourContractMatchingMeth.val.asClosure.chunk, contractMeth.Value.val.asClosure.chunk))
                            return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            //todo
            foreach (var contractVar in contract.FieldNames)
            {
                if (!lhs.Fields.TryGetValue(contractVar, out var _))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ValidateInstanceMeetsInstance(InstanceInternal lhs, InstanceInternal contract)
        {
            //todo confirm instance is same type
            if (lhs.GetType() != contract.GetType())
                return false;

            if ((lhs.FromClass == null || lhs.FromClass is DynamicClass)
                && (contract.FromClass == null || contract.FromClass is DynamicClass))
            {
                foreach (var field in contract.Fields.AsReadOnly)
                {
                    Value ourMatch = Value.Null();
                    if (lhs.Fields.TryGetValue(field.Key, out ourMatch)
                        || lhs.FromClass.Methods.TryGetValue(field.Key, out ourMatch))
                    {
                        if (field.Value.type != ourMatch.type)
                            return false;

                        switch (field.Value.type)
                        {
                        case ValueType.Closure:
                            var contractMeth = field.Value.val.asClosure.chunk;
                            if (!ChunkMatcher(ourMatch.val.asClosure.chunk, contractMeth))
                                return false;
                            break;
                        case ValueType.Instance:
                            if (!ValidateInstanceMeetsInstance(ourMatch.val.asInstance, field.Value.val.asInstance))
                                return false;
                            break;
                        default:
                            break;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }

            return ValidateInstanceMeetsClass(lhs, contract.FromClass);
        }

        private static bool ChunkMatcher(Chunk lhsMatchingChunk, Chunk contractChunk)
        {
            var contractMethArity = contractChunk.Arity;
            var ourArity = lhsMatchingChunk.Arity;
            if (ourArity != contractMethArity)
                return false;

            if (contractChunk.IsLocal && !lhsMatchingChunk.IsLocal)
                return false;

            if (contractChunk.IsPure && !lhsMatchingChunk.IsPure)
                return false;

            return true;
        }
    }
}
