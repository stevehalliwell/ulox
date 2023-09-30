using System.Collections.Generic;

namespace ULox
{
    public sealed class TypeInfo
    {
        private Dictionary<string, TypeInfoEntry> _userTypes = new Dictionary<string, TypeInfoEntry>();

        public int UserTypeCount => _userTypes.Count;

        public IEnumerable<TypeInfoEntry> Types => _userTypes.Values;

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
        private List<(Chunk chunk, byte labelID)> _initChains = new List<(Chunk, byte)>();
        public string Name => _name;
        public IReadOnlyList<Chunk> Methods => _methods;
        public IReadOnlyList<string> Fields => _fields;
        public IReadOnlyList<TypeInfoEntry> Mixins => _mixins;
        public IReadOnlyList<string> Contracts => _contracts;
        public IReadOnlyList<(Chunk chunk, byte labelID)> InitChains => _initChains;
        public UserType UserType { get; private set; }

        public TypeInfoEntry(string name, UserType userType)
        {
            _name = name;
            UserType = userType;
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
        }

        public void AddInitChain(Chunk currentChunk, byte initChainLabelId)
        {
            _initChains.Add((currentChunk, initChainLabelId));
        }
    }
}
