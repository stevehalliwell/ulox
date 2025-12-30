using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    //TODO: if class instances still carry their own stuff around, perhaps they could just become one of these?
    public sealed class TypeInfo
    {
        private readonly Dictionary<string, TypeInfoEntry> _userTypes = new();

        public int UserTypeCount => _userTypes.Count;

        public bool AllowTypeReplacement { get; set; } = false;

        public IEnumerable<TypeInfoEntry> Types => _userTypes.Values;

        public void AddType(TypeInfoEntry typeInfoEntry, Compiler compiler)
        {
            if (AllowTypeReplacement)
            {
                _userTypes[typeInfoEntry.Name] = typeInfoEntry;
                return;
            }
            try
            {
                _userTypes.Add(typeInfoEntry.Name, typeInfoEntry);
            }
            catch (System.Exception)
            {
                compiler.ThrowCompilerException($"Type of name '{typeInfoEntry.Name}' is already defined.");
            }
        }

        public TypeInfoEntry GetUserType(string v)
        {
            //todo move out of here so we get better context in the exception
            if (_userTypes.TryGetValue(v, out var res))
                return res;

            throw new UloxException($"Type of name '{v}' is not found.");
        }
    }

    public sealed class TypeInfoEntry
    {
        private readonly string _name;
        private readonly List<Chunk> _methods = new();
        private readonly List<string> _fields = new();
        private readonly List<string> _staticFields = new();
        private readonly List<string> _contracts = new();
        private readonly List<TypeInfoEntry> _mixins = new();
        private readonly List<(Chunk chunk, Label labelID)> _initChains = new();
        public string Name => _name;
        public IReadOnlyList<Chunk> Methods => _methods;
        public IReadOnlyList<string> Fields => _fields;
        public IReadOnlyList<string> StaticFields => _staticFields;
        public IReadOnlyList<TypeInfoEntry> Mixins => _mixins;
        public IReadOnlyList<string> Contracts => _contracts;
        public IReadOnlyList<(Chunk chunk, Label labelID)> InitChains => _initChains;
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
            _staticFields.AddRange(targetTypeInfoEntry.StaticFields);
            _methods.AddRange(targetTypeInfoEntry.Methods);
            _contracts.AddRange(targetTypeInfoEntry.Contracts);
            _initChains.AddRange(targetTypeInfoEntry.InitChains);
        }

        public void PrependInitChain(Chunk chunk, Label labelID)
        {
            if (_initChains.Any(x => x.chunk == chunk)) return;

            _initChains.Insert(0, (chunk, labelID));
        }

        public void AddStaticField(string name)
        {
            _staticFields.Add(name);
        }
    }
}
