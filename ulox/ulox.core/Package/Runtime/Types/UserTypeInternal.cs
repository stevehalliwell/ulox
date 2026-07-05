using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public class UserTypeInternal : InstanceInternal
    {
        public Table Methods { get; private set; } = new Table();

        public HashedString Name { get; protected set; }

        public UserType UserType { get; }
        public Value Initialiser { get; protected set; } = Value.Null();
        public List<(Chunk chunk, Label labelID)> InitChains { get; protected set; } = new();
        public IReadOnlyList<HashedString> FieldNames => _fieldsNames;
        private readonly List<HashedString> _fieldsNames = new();
        protected TypeInfoEntry _typeInfoEntry;

        public UserTypeInternal(HashedString name, UserType userType)
        {
            Name = name;
            UserType = userType;
        }

        public UserTypeInternal(TypeInfoEntry type)
        {
            _typeInfoEntry = type;

            Name = new HashedString(type.Name);
            UserType = type.UserType;
        }

        public void PrepareFromType(Vm vm)
        {
            foreach (var field in _typeInfoEntry.Fields)
            {
                AddFieldName(new HashedString(field));
            }

            foreach (var staticField in _typeInfoEntry.StaticFields)
            {
                Fields.AddOrSet(new HashedString(staticField), Value.Null());
            }

            foreach (var (chunk, labelId) in _typeInfoEntry.InitChains)
            {
                AddInitChain(chunk, labelId);
            }

            foreach (var method in _typeInfoEntry.Methods)
            {
                var methodValue = Value.New(new ClosureInternal { chunk = method });
                AddMethod(new HashedString(method.ChunkName), methodValue, vm);
            }

            Freeze();
            if (UserType == UserType.Enum)
                ReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual InstanceInternal MakeInstance()
        {
            var ret = new InstanceInternal(this);

            foreach (var fieldName in _fieldsNames)
            {
                ret.SetField(fieldName, Value.Null());
            }

            ret.Freeze();
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddMethod(HashedString key, Value method, Vm vm)
        {
            // This is used internally during type construction so does not need to check for frozen
            if (Methods.Get(key, out var existing))
            {
                vm.ThrowRuntimeException($"Cannot {nameof(AddMethod)} on {this}, already contains method '{existing}'.");
            }

            Methods.AddOrSet(key, method);

            if (key == ClassTypeCompilette.InitMethodName)
            {
                Initialiser = method;
            }
        }

        public void AddInitChain(Chunk chunk, Label labelID)
        {
            // This is used internally by the vm only does not need to check for frozen
            if (InitChains.Any(x => x.chunk == chunk)) return;

            InitChains.Add((chunk, labelID));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFieldName(HashedString fieldName)
            => _fieldsNames.Add(fieldName);

        public override string ToString() => $"<{UserType} {Name}>";
    }
}
