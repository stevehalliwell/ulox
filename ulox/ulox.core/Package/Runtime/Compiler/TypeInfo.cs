using System;
using System.Collections.Generic;

namespace ULox
{
    public sealed class TypeInfo
    {
        private Dictionary<string, TypeInfoEntry> _userTypes = new Dictionary<string, TypeInfoEntry>();

        public int UserTypeCount => _userTypes.Count;

        public void AddType(TypeInfoEntry typeInfoEntry)
        {
            _userTypes.Add(typeInfoEntry.Name, typeInfoEntry);
        }

        public TypeInfoEntry GetUserType(string v)
        {
            if (_userTypes.TryGetValue(v, out var res))
                return res;

            throw new UloxException($"Type of name '{v}' is not found.");
        }
    }

    public sealed class TypeInfoEntry
    {
        private string _name;
        private List<Chunk> _methods = new List<Chunk>();
        private List<string> _fields = new List<string>();
        private List<string> _contracts = new List<string>();
        private List<TypeInfoEntry> _mixins = new List<TypeInfoEntry>();
        private List<byte> _initChainLabelIds = new List<byte>();
        public string Name => _name;
        public IReadOnlyList<Chunk> Methods => _methods;
        public IReadOnlyList<string> Fields => _fields;
        public IReadOnlyList<TypeInfoEntry> Mixins => _mixins;
        public IReadOnlyList<string> Contracts => _contracts;

        public TypeInfoEntry(string name)
        {
            _name = name;
        }

        public void AddMethod(Chunk chunk)
        {
            _methods.Add(chunk);
        }

        public void AddField(string name)
        {
            _fields.Add(name);
        }

        public void AddContract(string name)
        {
            _contracts.Add(name);
        }

        public void AddMixin(TypeInfoEntry targetTypeInfoEntry)
        {
            _mixins.Add(targetTypeInfoEntry);
            _fields.AddRange(targetTypeInfoEntry.Fields);
            _methods.AddRange(targetTypeInfoEntry.Methods);
            _contracts.AddRange(targetTypeInfoEntry.Contracts);
            _initChainLabelIds.AddRange(targetTypeInfoEntry._initChainLabelIds);
        }

        public void AddInitChainLabelId(byte initChainLabelId)
        {
            _initChainLabelIds.Add(initChainLabelId);
        }
    }
}
