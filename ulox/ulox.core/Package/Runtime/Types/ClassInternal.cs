﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public class ClassInternal : InstanceInternal
    {
        public const int FirstMathOp = (int)OpCode.ADD;
        public const int LastMathOp = (int)OpCode.MODULUS;

        public static readonly HashedString[] MathOperatorMethodNames = new HashedString[]
        {
            new HashedString("_add"),
            new HashedString("_sub"),
            new HashedString("_mul"),
            new HashedString("_div"),
            new HashedString("_mod"),
        };

        public const int FirstCompOp = (int)OpCode.EQUAL;
        public const int LastCompOp = (int)OpCode.GREATER;

        public static readonly HashedString[] ComparisonOperatorMethodNames = new HashedString[]
        {
            new HashedString("_eq"),
            new HashedString("_ls"),
            new HashedString("_gr"),
        };

        private readonly Table methods = new Table();
        private readonly Table flavours = new Table();
        private readonly Value[] mathOperators = new Value[LastMathOp - FirstMathOp + 1];
        private readonly Value[] compOperators = new Value[LastCompOp - FirstCompOp + 1];

        //TODO these props also need to be write protected by the freeze
        public HashedString Name { get; protected set; }

        public Value Initialiser { get; protected set; } = Value.Null();
        public ClassInternal Super { get; protected set; }
        public List<(ClosureInternal, int)> InitChains { get; protected set; } = new List<(ClosureInternal, int)>();
        public IReadOnlyDictionary<HashedString, Value> Methods => methods.AsReadOnly;

        public ClassInternal(HashedString name)
        {
            Name = name;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual InstanceInternal MakeInstance()
        {
            return new InstanceInternal(this);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetMethod(HashedString name) => methods[name];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetMethod(HashedString name, out Value method) => methods.TryGetValue(name, out method);

        public void AddMethod(HashedString key, Value method)
        {
            CanWrite();
            methods[key] = method;
            if (key == TypeCompilette.InitMethodName)
            {
                Initialiser = method;
            }
            var opIndex = System.Array.FindIndex(MathOperatorMethodNames, x => key.Hash == x.Hash);
            if (opIndex != -1)
            {
                mathOperators[opIndex] = method;
            }
            else
            {
                opIndex = System.Array.FindIndex(ComparisonOperatorMethodNames, x => key.Hash == x.Hash);
                if (opIndex != -1)
                {
                    compOperators[opIndex] = method;
                }
            }
        }

        public void CanWrite()
        {
            if (IsFrozen)
                throw new FreezeException($"Attempted to modify frozen class '{Name}'.");
        }

        public void AddInitChain(ClosureInternal closure, ushort initChainStartOp)
        {
            CanWrite();
            InitChains.Add((closure, initChainStartOp));
        }

        public void InheritFrom(ClassInternal superClass)
        {
            CanWrite();
            Super = superClass;
            foreach (var item in superClass.methods)
            {
                var k = item.Key;
                var v = item.Value;
                AddMethod(k, v);
            }
        }

        public void AddMixin(Value flavourValue)
        {
            CanWrite();
            var flavour = flavourValue.val.asClass;
            flavours[flavour.Name] = flavourValue;

            foreach (var flavourMeth in flavour.methods)
            {
                MixinMethod(flavourMeth.Key, flavourMeth.Value);
            }

            foreach (var flavourInitChain in flavour.InitChains)
            {
                if (!InitChains.Contains(flavourInitChain))
                {
                    InitChains.Add(flavourInitChain);
                }
            }
        }

        private void MixinMethod(HashedString key, Value value)
        {
            if (methods.TryGetValue(key, out var existing))
            {
                //combine
                if (existing.type == ValueType.Closure)
                {
                    //make a combine
                    var temp = Value.Combined();
                    temp.val.asCombined.Add(existing.val.asClosure);
                    temp.val.asCombined.Add(value.val.asClosure);
                    existing = temp;
                }
                else
                {
                    existing.val.asCombined.Add(value.val.asClosure);
                }

                value = existing;
            }

            AddMethod(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetMathOpClosure(OpCode opCode)
        {
            int opIndex = (int)opCode - ClassInternal.FirstMathOp;
            return mathOperators[opIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetCompareOpClosure(OpCode opCode)
        {
            int opIndex = (int)opCode - ClassInternal.FirstCompOp;
            return compOperators[opIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void FinishCreation(InstanceInternal inst)
            => inst.Freeze();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ValidateClassMeetsClass(ClassInternal contract)
        {
            foreach (var contractMeth in contract.methods)
            {
                if (!methods.TryGetValue(contractMeth.Key, out var ourContractMatchingMeth))
                    throw new VMException($"Meets violation. '{Name.String}' meets '{contract.Name.String}' does not contain matching method '{contractMeth.Key.String}'.");

                var contractChunk = contractMeth.Value.val.asClosure.chunk;
                var contractMethArity = contractChunk.Arity;
                var ourContractMatchingChunk = ourContractMatchingMeth.val.asClosure.chunk;
                var ourArity = ourContractMatchingChunk.Arity;
                if (ourArity != contractMethArity)
                    throw new VMException($"Meets violation. '{Name.String}' meets '{contract.Name.String}' has method '{contractMeth.Key.String}' but expected arity of '{contractMethArity}' but has '{ourArity}'.");
            
                if(contractChunk.IsLocal && !ourContractMatchingChunk.IsLocal)
                    throw new VMException($"Meets violation. '{Name.String}' meets '{contract.Name.String}' expected local but is of type '{ourContractMatchingChunk.FunctionType}'.");

                if (contractChunk.IsPure && !ourContractMatchingChunk.IsPure)
                    throw new VMException($"Meets violation. '{Name.String}' meets '{contract.Name.String}' expected pure but is of type '{ourContractMatchingChunk.FunctionType}'.");
            }
        }
    }
}
