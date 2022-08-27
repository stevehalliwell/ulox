using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public class TestRunner
    {
        private readonly Dictionary<string, bool> _testStatus = new Dictionary<string, bool>();
        private string _lastId;

        public TestRunner(Func<Vm> createVM)
        {
            CreateVM = createVM;
        }

        public bool Enabled { get; set; } = true;
        public bool AllPassed => !_testStatus.Any(x => !x.Value);
        public int TestsFound => _testStatus.Count;
        public HashedString CurrentTestSetName { get; set; } = new HashedString(string.Empty);
        public Func<Vm> CreateVM { get; private set; }

        public void StartTest(string name)
        {
            var id = MakeId(name);
            if (_testStatus.ContainsKey(id))
                throw new TestRunnerException($"{nameof(TestRunner)} found a duplicate test '{id}'.");

            _testStatus[id] = false;
            _lastId = id;
        }

        public void EndTest(string name)
        {
            var id = MakeId(name);
            if (_testStatus.ContainsKey(id))
                _testStatus[id] = true;
            else if(_lastId.StartsWith(id))
                _testStatus[_lastId] = true;

            _lastId = null;
        }
        
        private string MakeId(string name)
        {
            return $"{CurrentTestSetName}:{name}";
        }
        
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
                StartTest(stringName);
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
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string ArgDescriptionString(Vm vm, byte argCount)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < argCount; i++)
            {
                sb.Append(vm.Peek(i));
                if (i < argCount - 1)
                    sb.Append(", ");
            }
            return sb.ToString();
        }

        private void DoTestSet(Vm vm, Chunk chunk)
        {
            var name = chunk.ReadConstant(vm.ReadByte(chunk)).val.asString;
            var testcaseCount = vm.ReadByte(chunk);

            CurrentTestSetName = name;

            for (int i = 0; i < testcaseCount; i++)
            {
                var loc = vm.ReadUShort(chunk);
                if (Enabled)
                {
                    RunTestCase(vm, chunk, loc);
                }
            }
        }

        protected virtual void RunTestCase(Vm vm, Chunk chunk, ushort loc)
        {
            try
            {
                var childVM = CreateVM();
                childVM.CopyFrom(vm);
                childVM.Interpret(chunk, loc);
            }
            catch (PanicException)
            {
                //eat it, results in incomplete test
            }
        }
    }
}
