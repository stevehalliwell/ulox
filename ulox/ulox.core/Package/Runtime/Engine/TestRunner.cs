using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public class TestRunner
    {
        private readonly Dictionary<string, bool> tests = new Dictionary<string, bool>();

        public TestRunner(Func<Vm> createVM)
        {
            CreateVM = createVM;
        }

        public bool Enabled { get; set; } = true;
        public bool AllPassed => !tests.Any(x => !x.Value);
        public int TestsFound => tests.Count;
        public HashedString CurrentTestSetName { get; set; } = new HashedString(string.Empty);
        public Func<Vm> CreateVM { get; private set; }

        public void StartTest(HashedString name)
        {
            var id = $"{CurrentTestSetName}:{name}";
            if (tests.ContainsKey(id))
                throw new TestRunnerException($"{nameof(TestRunner)} found a duplicate test '{id}'.");

            tests[id] = false;
        }

        public void EndTest(HashedString name) 
            => tests[$"{CurrentTestSetName}:{name}"] = true;

        public string GenerateDump()
        {
            var sb = new StringBuilder();

            foreach (var item in tests)
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
                StartTest(chunk.ReadConstant(vm.ReadByte(chunk)).val.asString);
                vm.ReadByte(chunk);//byte we don't use
                break;

            case TestOpType.CaseEnd:
                EndTest(chunk.ReadConstant(vm.ReadByte(chunk)).val.asString);
                vm.ReadByte(chunk);//byte we don't use
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
