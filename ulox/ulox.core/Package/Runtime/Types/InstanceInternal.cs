using System;
using System.Runtime.CompilerServices;

namespace ULox
{
    public class InstanceInternal
    {
        public InstanceInternal() 
            : this(null)
        {
        }

        public InstanceInternal(ClassInternal fromClass)
        {
            FromClass = fromClass;
        }

        public ClassInternal FromClass { get; protected set; }
        public bool IsFrozen { get; private set; } = false;
        public Table Fields { get; protected set; } = new Table();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasField(HashedString key) => Fields.ContainsKey(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetField(HashedString key, Value val)
        {
            if (!IsFrozen || Fields.ContainsKey(key))
                Fields[key] = val;
            else
                throw new FreezeException($"Attempted to Create a new field '{key}' via SetField on a frozen object. This is not allowed.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetField(HashedString key)
        {
            if (Fields.TryGetValue(key, out var ret))
                return ret;

            throw new System.Exception();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetField(HashedString key, out Value val) 
            => Fields.TryGetValue(key, out val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveField(HashedString fieldNameStr) 
            => Fields.Remove(fieldNameStr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Freeze()
            => IsFrozen = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unfreeze() 
            => IsFrozen = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(InstanceInternal inst)
        {
            FromClass = inst.FromClass;
            foreach (var keyPair in inst.Fields)
            {
                Fields[keyPair.Key] = keyPair.Value;
            }
            IsFrozen = inst.IsFrozen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ValidateInstanceMeetsClass(ClassInternal contract)
        {
            foreach (var contractMeth in contract.Methods)
            {
                Value ourContractMatchingMeth = Value.Null();
                if (Fields.TryGetValue(contractMeth.Key, out ourContractMatchingMeth)
                    || FromClass.Methods.TryGetValue(contractMeth.Key, out ourContractMatchingMeth))
                {
                    if (contractMeth.Value.type == ValueType.Closure
                        && ourContractMatchingMeth.type == ValueType.Closure)
                    {
                        if (!ValidateHelper(contractMeth.Value.val.asClosure.chunk, ourContractMatchingMeth.val.asClosure.chunk))
                            return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ValidateInstanceMeetsInstance(InstanceInternal asInstance)
        {
            foreach (var field in asInstance.Fields.AsReadOnly)
            {
                switch (field.Value.type)
                {
                case ValueType.Closure:
                    Value ourContractMatchingMeth = Value.Null();
                    var contractMeth = field.Value.val.asClosure.chunk;

                    if (Fields.TryGetValue(field.Key, out ourContractMatchingMeth)
                    || FromClass.Methods.TryGetValue(field.Key, out ourContractMatchingMeth))
                    {
                        if (!ValidateHelper(contractMeth, ourContractMatchingMeth.val.asClosure.chunk))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                default:
                    break;
                }
            }
            //todo check class

            return ValidateInstanceMeetsClass(asInstance.FromClass);
        }

        private bool ValidateHelper(Chunk contractChunk, Chunk ourContractMatchingChunk)
        {
            var contractMethArity = contractChunk.Arity;
            var ourArity = ourContractMatchingChunk.Arity;
            if (ourArity != contractMethArity)
                return false;

            if (contractChunk.IsLocal && !ourContractMatchingChunk.IsLocal)
                return false;

            if (contractChunk.IsPure && !ourContractMatchingChunk.IsPure)
                return false;

            return true;
        }
    }
}
