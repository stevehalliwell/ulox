using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public sealed class EnumValues
    {
        private readonly Dictionary<int, Value> _values = new Dictionary<int, Value>();
        private readonly Dictionary<Value, int> _keys = new Dictionary<Value, int>();
    }
    public sealed class EnumLookup
    {
        private readonly Dictionary<Value, EnumValues> _enums = new Dictionary<Value, EnumValues>();
    }
    public sealed class TestRunner
    {
        private readonly Dictionary<string, bool> _testStatus = new Dictionary<string, bool>();
        private string _lastId;
        private ushort _fixtureLoc;

        public TestRunner(Func<Vm> createVM)
        {
            CreateVM = createVM;
        }

        public bool Enabled { get; set; } = true;
        public bool AllPassed => !_testStatus.Any(x => !x.Value);
        public int TestsFound => _testStatus.Count;
        public HashedString CurrentTestSetName { get; set; } = new HashedString(string.Empty);
        public Func<Vm> CreateVM { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartTest(IVm vm, string name)
        {
            var id = MakeId(name);
            if (_testStatus.ContainsKey(id))
                vm.ThrowRuntimeException($"{nameof(TestRunner)} found a duplicate test '{id}'");

            _testStatus[id] = false;
            _lastId = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndTest(string name)
        {
            var id = MakeId(name);
            if (_testStatus.ContainsKey(id))
                _testStatus[id] = true;
            else if(_lastId.StartsWith(id))
                _testStatus[_lastId] = true;

            _lastId = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string MakeId(string name)
        {
            return $"{CurrentTestSetName}:{name}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GenerateDump()
        {
            var sb = new StringBuilder();

            foreach (var item in _testStatus)
            {
                sb.AppendLine($"{item.Key} {(item.Value ? "Completed" : "Incomplete")}");
            }

            return sb.ToString().Trim();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoTestOpCode(Vm vm, Chunk chunk)
        {
            var testOpType = (TestOpType)vm.ReadByte(chunk);
            switch (testOpType)
            {
            case TestOpType.CaseStart:
            {
                var nameId = vm.ReadByte(chunk);
                var argCount = vm.ReadByte(chunk);
                var stringName = chunk.ReadConstant(nameId).val.asString.String;
                if (argCount != 0)
                {
                    stringName += $"({ArgDescriptionString(vm, argCount)})";
                }
                StartTest(vm, stringName);
            }
            break;

            case TestOpType.CaseEnd:
            {
                var nameId = vm.ReadByte(chunk);
                var argCount = vm.ReadByte(chunk);
                var stringName = chunk.ReadConstant(nameId).val.asString.String;
                EndTest(stringName);
            }
            break;

            case TestOpType.TestSetStart:
                DoTestSet(vm, chunk);
                break;

            case TestOpType.TestSetEnd:
                CurrentTestSetName = new HashedString(string.Empty);
                vm.ReadByte(chunk);//byte we don't use
                vm.ReadByte(chunk);//byte we don't use
                SetFixtureLoc(ushort.MaxValue);
                break;
            case TestOpType.TestFixtureBodyInstruction:
                var labelId = vm.ReadByte(chunk);
                var loc = (ushort)chunk.Labels[labelId];
                SetFixtureLoc(loc);
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFixtureLoc(ushort loc)
        {
            _fixtureLoc = loc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string ArgDescriptionString(Vm vm, byte argCount)
        {
            var sb = new StringBuilder();
            for (int i = argCount - 1; i >= 0; i--)
            {
                sb.Append(vm.Peek(i));
                if (i != 0)
                    sb.Append(", ");
            }
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoTestSet(Vm vm, Chunk chunk)
        {
            var name = chunk.ReadConstant(vm.ReadByte(chunk)).val.asString;
            var testcaseCount = vm.ReadByte(chunk);

            CurrentTestSetName = name;

            for (int i = 0; i < testcaseCount; i++)
            {
                var label = vm.ReadByte(chunk);
                var loc = (ushort)chunk.Labels[label];
                if (Enabled)
                {
                    RunTestCase(vm, chunk, loc);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RunTestCase(Vm vm, Chunk chunk, ushort instructionLoc)
        {
            try
            {
                var childVM = CreateVM();
                childVM.CopyFrom(vm);
                childVM.CopyStackFrom(vm);
                childVM.MoveInstructionPointerTo(_fixtureLoc);
                childVM.Run();
                childVM.MoveInstructionPointerTo(instructionLoc);
                childVM.Run();
            }
            catch (PanicException)
            {
                //eat it, results in incomplete test
            }
        }
    }
}
